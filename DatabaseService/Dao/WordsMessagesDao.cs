using System.Text;
using DatabaseService.Data;
using Npgsql;
using NpgsqlTypes;

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
            const string commandText = """
                                       INSERT INTO Words_Messages (message_id, word_id, count, term_frequency) VALUES (@message_id, @word_id, @count, @term_frequency)
                                       ON CONFLICT (message_id, word_id) DO UPDATE
                                       SET count = excluded.count
                                       """;
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("message_id", wm.MessageId);
            cmd.Parameters.AddWithValue("word_id", wm.WordId);
            cmd.Parameters.AddWithValue("count", wm.Count);
            cmd.Parameters.AddWithValue("term_frequency", wm.TermFrequency);

            await cmd.ExecuteNonQueryAsync();
        }

        // public async Task AddSeveral(IEnumerable<WordMessage> wms)
        // {
        //     var trans = await _connection.BeginTransactionAsync();
        //     const string commandText =
        //         $"INSERT INTO {TableName} (message_id, word_id, count) VALUES (@message_id, @word_id, @count)" +
        //         "ON CONFLICT (message_id, word_id) DO UPDATE\n" +
        //         "SET count = excluded.count";
        //     foreach (var wm in wms)
        //     {
        //         await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
        //         cmd.Parameters.AddWithValue("message_id", wm.MessageId);
        //         cmd.Parameters.AddWithValue("word_id", wm.WordId);
        //         cmd.Parameters.AddWithValue("count", wm.Count);
        //
        //         await cmd.ExecuteNonQueryAsync();
        //     }
        //
        //     await trans.CommitAsync();
        // }

        public async Task AddSeveral(IEnumerable<WordMessage> wms)
        {
            var sb = new StringBuilder();
            var parameters = new List<NpgsqlParameter>();
            int i = 0;

            foreach (var wordMessage in wms)
            {
                var mName = $"messageId{i}";
                parameters.Add(new NpgsqlParameter(mName, NpgsqlDbType.Bigint)
                {
                    Value = wordMessage.MessageId
                });
                var wName = $"wordId{i}";
                parameters.Add(new NpgsqlParameter(wName, NpgsqlDbType.Integer)
                {
                    Value = wordMessage.WordId
                });
                var cName = $"count{i}";
                parameters.Add(new NpgsqlParameter(cName, NpgsqlDbType.Integer)
                {
                    Value = wordMessage.Count
                });
                var tfName = $"tf{i}";
                parameters.Add(new NpgsqlParameter(tfName, NpgsqlDbType.Real)
                {
                    Value = wordMessage.TermFrequency
                });

                sb.Append('(');
                sb.Append(':');
                sb.Append(mName);
                sb.Append(", :");
                sb.Append(wName);
                sb.Append(", :");
                sb.Append(cName);
                sb.Append(", :");
                sb.Append(tfName);
                sb.Append("), ");

                i++;
            }

            if (i == 0)
            {
                return;
            }


            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            var valuesString = sb.ToString();

            var commandText = $"""
                               INSERT INTO words_messages (message_id, word_id, count, term_frequency)
                               VALUES {valuesString}
                               ON CONFLICT(message_id, word_id)
                               DO UPDATE SET count = excluded.count
                               """;


            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddRange(parameters.ToArray());

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddWordsForLastMessage(Dictionary<string, int> input)
        {
            var parameters = new NpgsqlParameter[input.Count * 2];
            var sb = new StringBuilder();

            var i = 0;
            foreach (var (word, count) in input)
            {
                var wName = $"w{i}";
                var cName = $"c{i}";
                parameters[i * 2] = new NpgsqlParameter(wName, NpgsqlDbType.Varchar)
                {
                    Value = word
                };
                parameters[i * 2 + 1] = new NpgsqlParameter(cName, NpgsqlDbType.Integer)
                {
                    Value = count
                };

                sb.Append("@last_message_id, :");
                sb.Append(wName);

                i++;
            }
        }

        public async Task<WordMessage?> GetById(long id)
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

        public async Task<List<WordMessage>> GetByWord(string word)
        {
            var result = new List<WordMessage>();
            //const string commandText = $"SELECT * FROM {TableName} WHERE message_id = @message_id";
            const string commandText = """
                                       SELECT * FROM words_messages WHERE word_id in (
                                           SELECT id FROM words WHERE word = @word
                                       )
                                       """;


            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word", word);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(ReadWordMessage(reader));
            }

            return result;
        }

        private static WordMessage ReadWordMessage(NpgsqlDataReader reader)
        {
            if (reader["term_frequency"] is float termFrequency &&
                reader["message_id"] is long messageId &&
                reader["word_id"] is int wordId &&
                reader["count"] is int count &&
                reader["Id"] is long id
               )
            {
                return new WordMessage()
                {
                    Id = id,
                    MessageId = messageId,
                    WordId = wordId,
                    Count = count,
                    TermFrequency = termFrequency
                };
            }
            else
            {
                throw new Exception("Could not read WordMessage");
            }
        }
    }
}