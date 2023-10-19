using DatabaseService.Dao;
using DatabaseService.Data;
using Npgsql;

namespace DatabaseService
{
    public class Service : IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly SupergroupDao _supergroupDao;
        private readonly MessageDao _messageDao;
        private readonly WordDao _wordDao;

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
            _messageDao = new MessageDao(_connection);
            _wordDao = new WordDao(_connection);
        }

        public Task InsertSupergroup(Supergroup group) => _supergroupDao.Add(group);
        public Task InsertSupergroups(IEnumerable<Supergroup> groups) => _supergroupDao.AddSeveral(groups);
        public Task<Supergroup?> GetSupergroupById(long id) => _supergroupDao.GetById(id);

        public Task InsertMessage(Message message) => _messageDao.Add(message);
        public Task InsertMessages(IEnumerable<Message> messages) => _messageDao.AddSeveral(messages);
        public Task<Message?> GetMessageById(long id) => _messageDao.GetById(id);

        public void Dispose()
        {
            _connection.Close();
        }

    }
}