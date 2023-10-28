using System.Text;
using DatabaseService.Data;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseService.Dao;

public class MessageDao
{
    private readonly NpgsqlConnection _connection;
    private const string TableName = "Messages";

    public MessageDao(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task Add(Message message)
    {
        const string commandText =
            $"INSERT INTO {TableName} (telegram_id, sender, content) VALUES (@telegram_id, @sender, @content)" +
            "ON CONFLICT (telegram_id, sender) DO UPDATE\n" +
            "SET content = excluded.content";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("telegram_id", message.TelegramId);
        cmd.Parameters.AddWithValue("sender", message.Sender);
        cmd.Parameters.AddWithValue("content", message.Content);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddSeveral(IEnumerable<Message> messages)
    {
        var trans = await _connection.BeginTransactionAsync();
        const string commandText =
            $"INSERT INTO {TableName} (telegram_id, sender, content) VALUES (@telegram_id, @sender, @content)" +
            "ON CONFLICT (telegram_id, sender) DO UPDATE\n" +
            "SET content = excluded.content";
        foreach (var message in messages)
        {
            await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
            cmd.Parameters.AddWithValue("telegram_id", message.TelegramId);
            cmd.Parameters.AddWithValue("sender", message.Sender);
            cmd.Parameters.AddWithValue("content", message.Content);

            await cmd.ExecuteNonQueryAsync();
        }

        await trans.CommitAsync();
    }

    public async Task<Message?> GetById(long id)
    {
        const string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var message = ReadMessage(reader);
            return message;
        }

        return null;
    }

    public async Task<Message?> GetBySenderAndTelegramId(long sender, int telegramId)
    {
        const string commandText = $"SELECT * FROM {TableName} WHERE sender = @sender AND telegram_id = @telegram_id";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("sender", sender);
        cmd.Parameters.AddWithValue("telegram_id", telegramId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var message = ReadMessage(reader);
            return message;
        }

        return null;
    }

    public async Task<IList<Message>> GetContainingWords(IEnumerable<string> words)
    {
        var result = new List<Message>();
        const string commandTextTemplate = """
                                           SELECT *
                                           FROM messages
                                           WHERE id IN (
                                               SELECT message_id
                                               FROM words_messages
                                               WHERE word_id IN (
                                                   SELECT id
                                                   FROM words
                                                   WHERE words.word in  (
                                           """;
        var sb = new StringBuilder(commandTextTemplate);
        var parameters = new List<NpgsqlParameter>();
        int i = 0;
        foreach (var word in words)
        {
            var pName = $"p{i}";
            var p = new NpgsqlParameter(pName, NpgsqlDbType.Varchar);
            p.Value = word;
            parameters.Add(p);

            sb.Append(':');
            sb.Append(pName);
            sb.Append(", ");

            i++;
        }

        sb[^2] = ')';
        sb[^1] = ')';
        sb.Append(')');
        var commandText = sb.ToString();

        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddRange(parameters.ToArray());

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadMessage(reader));
        }

        return result;
    }

    private static Message ReadMessage(NpgsqlDataReader reader)
    {
        var readId = reader["Id"] as int?;
        var readSender = reader["sender"] as long?;
        var readContent = reader["content"] as string;
        var readTelegramId = reader["telegram_id"] as int?;

        if (readId == null ||
            readSender == null ||
            readContent == null ||
            readTelegramId == null)
        {
            throw new Exception("Could not read message");
        }

        var message = new Message()
        {
            Id = readId.Value,
            TelegramId = readTelegramId.Value,
            Sender = readSender.Value,
            Content = readContent,
        };
        return message;
    }
}