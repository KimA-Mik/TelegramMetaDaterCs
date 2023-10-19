using DatabaseService.Data;
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
        string commandText =
            $"INSERT INTO {TableName} (telegram_id, sender, content) VALUES (@telegram_id, @sender, @content)" +
            "ON CONFLICT (telegram_id, sender) DO UPDATE\n" +
            "SET content = excluded.content";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("telegram_id", message.telegramId);
        cmd.Parameters.AddWithValue("sender", message.sender);
        cmd.Parameters.AddWithValue("content", message.content);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddSeveral(IEnumerable<Message> messages)
    {
        var trans = await _connection.BeginTransactionAsync();
        string commandText =
            $"INSERT INTO {TableName} (telegram_id, sender, content) VALUES (@telegram_id, @sender, @content)" +
            "ON CONFLICT (telegram_id, sender) DO UPDATE\n" +
            "SET content = excluded.content";
        foreach (var message in messages)
        {
            await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
            cmd.Parameters.AddWithValue("telegram_id", message.telegramId);
            cmd.Parameters.AddWithValue("sender", message.sender);
            cmd.Parameters.AddWithValue("content", message.content);

            await cmd.ExecuteNonQueryAsync();
        }

        await trans.CommitAsync();
    }

    public async Task<Message?> GetById(long id)
    {
        string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Message message = ReadMessage(reader);
            return message;
        }

        return null;
    }

    public async Task<int> GetdBySenderAndTelegramId(long sender, int telegramId)
    {
        string commandText = $"SELECT * FROM {TableName} WHERE sender = @sender AND telegram_id = @telegram_id";
        await using var cmd = new NpgsqlCommand(commandText, _connection);
        cmd.Parameters.AddWithValue("sender", sender);
        cmd.Parameters.AddWithValue("telegram_id", telegramId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Message message = ReadMessage(reader);
            return message.id;
        }

        return -1;
    }

    private static Message ReadMessage(NpgsqlDataReader reader)
    {
        int? readId = reader["Id"] as int?;
        int? readTelegramId = reader["telegram_id"] as int?;
        long? readSender = reader["Title"] as long?;
        string? readContent = reader["MainUsername"] as string;

        if (readId == null ||
            readSender == null ||
            readContent == null ||
            readTelegramId == null)
        {
            throw new Exception("Could not read supergroup");
        }

        Message message = new Message()
        {
            id = readId.Value,
            telegramId = readTelegramId.Value,
            sender = readSender.Value,
            content = readContent
        };
        return message;
    }
}