﻿using DatabaseService.Data;
using DatabaseService.Util;
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

        // public async Task AddSeveral(IEnumerable<string> words, int depth = 0)
        // {
        //     var trans = await _connection.BeginTransactionAsync();
        //     const string commandText = """
        //                                INSERT INTO words (word) VALUES (@word)
        //                                ON CONFLICT (word) DO NOTHING
        //                                """;
        //     foreach (var word in words)
        //     {
        //         await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
        //         cmd.Parameters.AddWithValue("word", word);
        //         await cmd.ExecuteNonQueryAsync();
        //     }
        //
        //     await trans.CommitAsync();
        // }

        public async Task AddSeveral(IEnumerable<string> words)
        {
            var parameters = DBUtil.StringsToValues(words, out var valuesString);
            if (parameters.Length == 0)
            {
                return;
            }

            var commandText = $"""
                               INSERT INTO words (word) VALUES {valuesString}
                               ON CONFLICT (word) DO NOTHING;
                               """;
            try
            {

                var cmd = new NpgsqlCommand(commandText, _connection);
                cmd.Parameters.AddRange(parameters);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(commandText);
                throw ex;
            }
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

        public async Task<IList<Word>> GetWordsByStrings(IEnumerable<string> words)
        {
            var result = new List<Word>();

            var parameters = DBUtil.StringsToParams(words, out var paramsString);
            if (paramsString.Length < 3)
            {
                return result;
            }

            var commandText = $"SELECT * FROM words WHERE words.word IN ({paramsString});";

            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

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
                Id = readId.Value,
                Text = readContent
            };
            return message;
        }
    }
}