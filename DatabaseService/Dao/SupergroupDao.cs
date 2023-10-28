using DatabaseService.Data;
using Npgsql;

namespace DatabaseService.Dao
{
    internal class SupergroupDao
    {
        private readonly NpgsqlConnection _connection;
        private const string TableName = "supergroups";

        public SupergroupDao(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task Add(Supergroup group)
        {
            const string commandText =
                $"INSERT INTO {TableName} (id, title, main_username) VALUES (@id, @title, @mainUsername)";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", group.Id);
            cmd.Parameters.AddWithValue("title", group.Title);
            cmd.Parameters.AddWithValue("mainUsername", group.MainUsername);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddSeveral(IEnumerable<Supergroup> groups)
        {
            await using var trans = await _connection.BeginTransactionAsync();
            const string commandText =
                $"INSERT INTO {TableName} (id, title, main_username) VALUES (@id, @title, @mainUsername)\n" +
                "ON CONFLICT (id) DO UPDATE\n" +
                "SET title = excluded.title," +
                "main_username = excluded.main_username";

            foreach (var group in groups)
            {
                await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
                cmd.Parameters.AddWithValue("id", group.Id);
                cmd.Parameters.AddWithValue("title", group.Title);
                cmd.Parameters.AddWithValue("mainUsername", group.MainUsername ?? "");
                await cmd.ExecuteNonQueryAsync();
            }

            await trans.CommitAsync();
        }

        public async Task<Supergroup?> GetById(long id)
        {
            const string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
            if (commandText == null) throw new ArgumentNullException(nameof(commandText));
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var group = ReadSupergroup(reader);
                return group;
            }

            return null;
        }

        private static Supergroup ReadSupergroup(NpgsqlDataReader reader)
        {
            var readId = reader["Id"] as long?;
            var readTitle = reader["Title"] as string;
            var readMainUsername = reader["MainUsername"] as string;

            if (readId == null ||
                readTitle == null ||
                readMainUsername == null)
            {
                throw new Exception("Could not read supergroup");
            }

            var group = new Supergroup
            {
                Id = readId.Value,
                Title = readTitle,
                MainUsername = readMainUsername
            };
            return group;
        }
    }
}