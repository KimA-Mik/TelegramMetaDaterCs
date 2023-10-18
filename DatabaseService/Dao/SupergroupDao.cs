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
            string commandText = $"INSERT INTO {TableName} (id, title, main_username) VALUES (@id, @title, @mainUsername)";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", group.id);
            cmd.Parameters.AddWithValue("title", group.title);
            cmd.Parameters.AddWithValue("mainUsername", group.mainUsername);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddSeveral(IEnumerable<Supergroup> groups)
        {
            using var trans = await _connection.BeginTransactionAsync();
            string commandText = $"INSERT INTO {TableName} (id, title, main_username) VALUES (@id, @title, @mainUsername)\n" +
                "ON CONFLICT (id) DO UPDATE\n" +
                "SET title = excluded.title," +
                "main_username = excluded.main_username";

            foreach (Supergroup group in groups)
            {
                if (group.mainUsername == null)
                {
                    continue;
                }
                await using var cmd = new NpgsqlCommand(commandText, _connection, trans);
                cmd.Parameters.AddWithValue("id", group.id);
                cmd.Parameters.AddWithValue("title", group.title);
                cmd.Parameters.AddWithValue("mainUsername", group.mainUsername);
                await cmd.ExecuteNonQueryAsync();
            }
            await trans.CommitAsync();
        }

        public async Task<Supergroup?> GetById(long id)
        {
            string commandText = $"SELECT * FROM {TableName} WHERE Id = @id";
            await using var cmd = new NpgsqlCommand(commandText, _connection);
            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Supergroup group = ReadSupergroup(reader);
                return group;
            }

            return null;
        }

        private static Supergroup ReadSupergroup(NpgsqlDataReader reader)
        {
            long? readId = reader["Id"] as long?;
            string? readTitle = reader["Title"] as string;
            string? readMainUsername = reader["MainUsername"] as string;

            if (readId == null ||
                readTitle == null ||
                readMainUsername == null)
            {
                throw new Exception("Could not read supergroup");
            }

            Supergroup group = new Supergroup
            {
                id = readId.Value,
                title = readTitle,
                mainUsername = readMainUsername
            };
            return group;
        }

    }
}
