using System.Data;
using System.Text;
using DatabaseService.Data;
using Npgsql;
using NpgsqlTypes;

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
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var res = ReadWord(reader);
                return res;
            }

            return null;
        }

        public async Task<IList<Word>> GetWordsByStrings(IList<string> words)
        {
            const string commandTextTemplate = "SELECT * FROM words WHERE words.word IN (";
            var sb = new StringBuilder(commandTextTemplate);
            var parameters = new NpgsqlParameter[words.Count];

            for (int i = 0 ; i < words.Count; ++i)
            {
                var pTitle = $"word{i}";
                parameters[i] = new NpgsqlParameter(pTitle, NpgsqlDbType.Varchar);
                parameters[i].Value = words[i];
                sb.Append(':');
                sb.Append(pTitle);
                sb.Append(", ");
            }

            sb[^2] = ')';
            var commandText = sb.ToString();
            Console.WriteLine(commandText);
            
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();
            var result = new List<Word>();

            while (await reader.ReadAsync())
            {
                result.Add(ReadWord(reader));
            }

            return result;
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