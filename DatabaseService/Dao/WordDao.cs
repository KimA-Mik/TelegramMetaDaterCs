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
            const string commandText = $"INSERT INTO {TableName} (word) VALUES (@word)";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word", word);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddSeveral(IEnumerable<string> words)
        {
            var trans = await _connection.BeginTransactionAsync();
            const string commandText = """
                                       INSERT INTO words (word) VALUES (@word)
                                       ON CONFLICT (word) DO NOTHING
                                       """;
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
            const string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var word = ReadWord(reader);
                return word;
            }

            return null;
        }

        public async Task<Word?> GetByWord(string word)
        {
            const string commandText = "SELECT * FROM words WHERE word = @word";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("word", word);
            cmd.CommandText = commandText;
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var res = ReadWord(reader);
                return res;
            }

            return null;
        }


        private static Word ReadWord(NpgsqlDataReader reader)
        {
            var readId = reader["Id"] as int?;
            var readContent = reader["word"] as string;

            if (readId == null ||
                readContent == null)
            {
                throw new Exception("Could not read word");
            }

            var message = new Word()
            {
                id = readId.Value,
                word = readContent
            };
            return message;
        }
    }
}