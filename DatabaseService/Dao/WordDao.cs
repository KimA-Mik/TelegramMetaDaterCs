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
    }
}
