using DatabaseService.Data;
using Npgsql;

namespace DatabaseService.Dao
{
    internal class WordsMessagesDao
    {
        private readonly NpgsqlConnection _connection;
        private const string TableName = "Words_Messages";

        public WordsMessagesDao(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task Add(WordMessage wm)
        {
            const string commandText =
                $"INSERT INTO {TableName} (message_id, word_id, count) VALUES (@message_id, @word_id, @count)" +
                "ON CONFLICT (message_id, word_id) DO UPDATE\n" +
                "SET count = excluded.count";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("message_id", wm.messageId);
            cmd.Parameters.AddWithValue("word_id", wm.wordId);
            cmd.Parameters.AddWithValue("count", wm.count);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddSeveral(IEnumerable<WordMessage> wms)
        {
            var trans = await _connection.BeginTransactionAsync();
            const string commandText =
                $"INSERT INTO {TableName} (message_id, word_id, count) VALUES (@message_id, @word_id, @count)" +
                "ON CONFLICT (message_id, word_id) DO UPDATE\n" +
                "SET count = excluded.count";
            foreach (var wm in wms)
            {
                await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
                cmd.Parameters.AddWithValue("message_id", wm.messageId);
                cmd.Parameters.AddWithValue("word_id", wm.wordId);
                cmd.Parameters.AddWithValue("count", wm.count);

                await cmd.ExecuteNonQueryAsync();
            }

            await trans.CommitAsync();
        }

        public async Task<WordMessage?> GetById(int id)
        {
            const string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var wm = ReadWordMessage(reader);
                return wm;
            }

            return null;
        }

        public async Task<IEnumerable<WordMessage>> GetByMessageId(long messageId)
        {
            var result = new List<WordMessage>();
            const string commandText = $"SELECT * FROM {TableName} WHERE message_id = @message_id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("message_id", messageId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var wm = ReadWordMessage(reader);
                result.Add(wm);
            }

            return result;
        }

        public async Task<IList<WordMessage>> GetByWordId(int wordId)
        {
            var result = new List<WordMessage>();
            const string commandText = $"SELECT * FROM {TableName} WHERE message_id = @message_id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word_id", wordId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var wm = ReadWordMessage(reader);
                result.Add(wm);
            }

            return result;
        }

        private static WordMessage ReadWordMessage(NpgsqlDataReader reader)
        {
            var readId = reader["Id"] as int?;
            var readMessageId = reader["message_id"] as long?;
            var readWordId = reader["word_id"] as int?;
            var readCount = reader["count"] as int?;

            if (readId == null ||
                readCount == null ||
                readWordId == null ||
                readMessageId == null)
            {
                throw new Exception("Could not read WordMessage");
            }

            var message = new WordMessage()
            {
                id = readId.Value,
                messageId = readMessageId.Value,
                wordId = readWordId.Value,
                count = readCount.Value
            };
            return message;
        }
    }
}