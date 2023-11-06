using DatabaseService.Data;
using DatabaseService.Util;
using Npgsql;

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
        const string commandText = """
                                   INSERT INTO messages (telegram_id, sender, content) VALUES (@telegram_id, @sender, @content)
                                   ON CONFLICT (telegram_id, sender) DO UPDATE
                                   SET content = excluded.content
                                   """;

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
        var parameters = DBUtil.StringsToParams(words, out var paramsString);
        string commandText = $"""
                              SELECT *
                              FROM messages
                              WHERE id IN (
                                  SELECT message_id
                                  FROM words_messages
                                  WHERE word_id IN (
                                      SELECT id
                                      FROM words
                                      WHERE words.word in  ({paramsString})
                                  )
                              )
                              """;

        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadMessage(reader));
        }

        return result;
    }

    public async Task<Message?> GetLastForSender(long senderId)
    {
        const string commandText = """
                                   SELECT * FROM messages WHERE sender = :senderId
                                   ORDER BY telegram_id DESC
                                   LIMIT 1
                                   """;

        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("senderId", senderId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            return ReadMessage(reader);
        }

        return null;
    }

    public async Task<Message?> GetFirstForSender(long senderId)
    {
        const string commandText = """
                                   SELECT * FROM messages WHERE sender = :senderId
                                   ORDER BY telegram_id ASC
                                   LIMIT 1
                                   """;

        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("senderId", senderId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            return ReadMessage(reader);
        }

        return null;
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