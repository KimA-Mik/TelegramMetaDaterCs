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
                                   INSERT INTO messages (telegram_id, sender, content, words_count) VALUES (@telegram_id, @sender, @content, @words_count)
                                   ON CONFLICT (telegram_id, sender) DO UPDATE
                                   SET content = excluded.content
                                   """;

        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("telegram_id", message.TelegramId);
        cmd.Parameters.AddWithValue("sender", message.Sender);
        cmd.Parameters.AddWithValue("content", message.Content);
        cmd.Parameters.AddWithValue("words_count", message.Words);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddSeveral(IEnumerable<Message> messages)
    {
        var trans = await _connection.BeginTransactionAsync();
        const string commandText =
            $"INSERT INTO {TableName} (telegram_id, sender, content, words_count) VALUES (@telegram_id, @sender, @content, @words_count)" +
            "ON CONFLICT (telegram_id, sender) DO UPDATE\n" +
            "SET content = excluded.content";
        foreach (var message in messages)
        {
            await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
            cmd.Parameters.AddWithValue("telegram_id", message.TelegramId);
            cmd.Parameters.AddWithValue("sender", message.Sender);
            cmd.Parameters.AddWithValue("content", message.Content);
            cmd.Parameters.AddWithValue("words_count", message.Words);

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

    public async Task<List<Message>> GetByIds(IEnumerable<long> ids)
    {
        var result = new List<Message>();
        var parameters = DBUtil.LongsToParams(ids, out var paramsString, "word");

        if (parameters.Length == 0)
        {
            return result;
        }

        var commandText = $"SELECT * FROM messages WHERE Id IN ({paramsString})";
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
                                   ORDER BY telegram_id
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

    public async Task<long> GetCount()
    {
        const string commandText = "SELECT COUNT(*) FROM messages";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader["count"] is long count)
            {
                return count;
            }
        }

        return 0;
    }

    private static Message ReadMessage(NpgsqlDataReader reader)
    {
        if (reader["words_count"] is int wordsCount &&
            reader["telegram_id"] is int telegramId &&
            reader["content"] is string content &&
            reader["sender"] is long sender &&
            reader["Id"] is long id
           )
        {
            return new Message()
            {
                Id = id,
                Sender = sender,
                Content = content,
                TelegramId = telegramId,
                Words = wordsCount
            };
        }

        throw new Exception("Could not read message");
    }
}