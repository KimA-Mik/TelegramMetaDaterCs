﻿using DatabaseService.Dao;
using DatabaseService.Data;
using Npgsql;

namespace DatabaseService
{
    public class Service : IDisposable
    {
        private readonly NpgsqlConnection _connection;

        private readonly WordsMessagesDao _wordsMessagesDao;
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

            _wordsMessagesDao = new WordsMessagesDao(_connection);
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
        public Task<List<Message>> GetMessagesByIds(IEnumerable<long> ids) => _messageDao.GetByIds(ids);
        public Task<Message?> GetLastMessage(long senderId) => _messageDao.GetLastForSender(senderId);
        public Task<Message?> GetFirstMessage(long senderId) => _messageDao.GetFirstForSender(senderId);
        public Task<long> GetMessagesCount() => _messageDao.GetCount();

        public Task<Message?> GetMessageBySenderAndTelegramId(long sender, int telegramId) =>
            _messageDao.GetBySenderAndTelegramId(sender, telegramId);

        public Task<IList<Message>> GetMessagesWithWords(IList<string> words) =>
            _messageDao.GetContainingWords(words);


        public Task InsertWord(string word) => _wordDao.Add(word);
        public Task InsertWords(IEnumerable<string> words) => _wordDao.AddSeveral(words);
        public Task<Word?> GetWordById(int id) => _wordDao.GetById(id);
        public Task<Word?> GetWordByWord(string word) => _wordDao.GetByWord(word);
        public Task<IList<Word>> GetWordsByStrings(IEnumerable<string> words) => _wordDao.GetWordsByStrings(words);

        public Task InsertWordMessage(WordMessage wm) => _wordsMessagesDao.Add(wm);
        public Task InsertWordMessages(IEnumerable<WordMessage> wms) => _wordsMessagesDao.AddSeveral(wms);
        public Task<WordMessage?> GetWordMessageById(long id) => _wordsMessagesDao.GetById(id);
        public Task<IList<WordMessage>> GetWmsForWord(int wordId) => _wordsMessagesDao.GetByWordId(wordId);

        public Task<IEnumerable<WordMessage>> GetWmsForMessage(long messageId) =>
            _wordsMessagesDao.GetByMessageId(messageId);

        public Task<List<WordMessage>> GetWmsForWord(string word) =>
            _wordsMessagesDao.GetByWord(word);


        public void Dispose()
        {
            _connection.Close();
        }
    }
}