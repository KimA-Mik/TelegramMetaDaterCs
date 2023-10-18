using DatabaseService.Dao;
using DatabaseService.Data;
using Npgsql;

namespace DatabaseService
{
    public class Service : IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly SupergroupDao _supergroupDao;

        public Service()
        {
            string host = Environment.GetEnvironmentVariable("postgres_host")!;
            string username = Environment.GetEnvironmentVariable("postgres_username")!;
            string password = Environment.GetEnvironmentVariable("postgres_password")!;
            string database = Environment.GetEnvironmentVariable("postgres_database")!;
            string connectionString = $"Host={host};Username={username};Password={password};Database={database}";

            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
            _supergroupDao = new SupergroupDao(_connection);
        }

        public Task InsertSupergrup(Supergroup group) => _supergroupDao.Add(group);
        public Task InsertSupergrups(IEnumerable<Supergroup> groups) => _supergroupDao.AddSeveral(groups);
        public Task<Supergroup?> GetSupergroupById(long id) => _supergroupDao.GetById(id);

        public void Dispose()
        {
            _connection.Close();
        }

    }
}