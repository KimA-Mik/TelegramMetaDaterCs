using DatabaseService.Data;
using Npgsql;

namespace DatabaseService.Dao
{
    internal class WordDao
    {
        private readonly NpgsqlConnection _connection;
        private const string TableName = "words";

        public WordDao(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task Add(string word)
        {
            string commandText = $"INSERT INTO {TableName} (word) VALUES (@word)";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word", word);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task AddSeveral(IEnumerable<string> words)
        {
            var trans = await _connection.BeginTransactionAsync();
            string commandText =
                $"INSERT INTO {TableName} (word) VALUES (@word)" +
                "ON CONFLICT (word) DO UPDATE\n" +
                "SET word = excluded.word";
            foreach (var word in words)
            {
                await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
                cmd.Parameters.AddWithValue("word", word);
                await cmd.ExecuteNonQueryAsync();
            }

            await trans.CommitAsync();
        }

        public async Task<Word?> GetById(int id)
        {
            string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var word = ReadWord(reader);
                return word;
            }
            return null;
        }

        public async Task<Word?> GetByWord(string word)
        {
            string commandText = $"SELECT * FROM {TableName} WHERE word = @word";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word", word);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var res = ReadWord(reader);
                return res;
            }
            return null;
        }


        private static Word ReadWord(NpgsqlDataReader reader)
        {
            int? readId = reader["Id"] as int?;
            string? readContent = reader["MainUsername"] as string;

            if (readId == null ||
                readContent == null)
            {
                throw new Exception("Could not read word");
            }

            Word message = new Word()
            {
                id = readId.Value,
                word = readContent
            };
            return message;
        }
    }
}
