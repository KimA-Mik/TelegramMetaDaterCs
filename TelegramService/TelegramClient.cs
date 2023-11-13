using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DatabaseService;
using DatabaseService.Data;
using TelegramService.Index;
using TL;
using WTelegram;
using Message = DatabaseService.Data.Message;

namespace TelegramService
{
    public class TelegramClient : IDisposable
    {
        private readonly string _phoneNumber;
        private readonly string _sessionFilePath;
        private readonly Client _client;
        private readonly Service _service;
        private readonly Indexer _indexer;
        private readonly ConcurrentQueue<Message> _outMessages = new();
        private readonly HashSet<long> excluded = new();

        private readonly Dictionary<long, User> _users = new();
        private readonly Dictionary<long, ChatBase> _chats = new();

        public bool IsActive { get; private set; }

        public TelegramClient(string phoneNumber, string sessionName, Service? service = null)
        {
            _phoneNumber = phoneNumber;
            //Sessions seems to need old method, idk 
            //fix it later
            var sessionFileName = sessionName + ".session";
            var currentDirectory = Directory.GetCurrentDirectory();
            var sessionsDirectory = Path.Combine(currentDirectory, "sessions");
            Directory.CreateDirectory(sessionsDirectory);
            _sessionFilePath = Path.Combine(sessionsDirectory, sessionFileName);

            LoadEnvironmentVariables(out var apiId, out var apiHash);

            _client = new Client(apiId, apiHash);
            _service = service ?? new Service();
            _indexer = new Indexer(_service);

            IsActive = true;

            excluded.Add(1628022511);
            excluded.Add(1361320510);
            excluded.Add(1712996713);
            excluded.Add(1138054947);
            excluded.Add(1196188017);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private int i = 0;

        public bool TryGetMessage([MaybeNullWhen(false)] out Message message)
        {
            i++;
            if (i % 100 == 0)
            {
                Console.WriteLine($"Queue size is {_outMessages.Count}");
            }

            return _outMessages.TryDequeue(out message);
        }

        public async Task Login()
        {
            var loginInfo = _phoneNumber;
            while (_client.User == null)
            {
                switch (await _client.Login(loginInfo))
                {
                    case "session_pathname":
                        loginInfo = _sessionFilePath;
                        break;
                    case "verification_code":
                        Console.Write("Code: ");
                        loginInfo = Console.ReadLine();
                        break;
                    default:
                        loginInfo = null;
                        break; // let WTelegramClient decide the default config
                }
            }

            Console.WriteLine($"We are logged-in as {_client.User} (id {_client.User.id})");
        }


        public async Task RunSth()
        {
            List<Supergroup> supergroups = new List<Supergroup>();
            var chats = await _client
                .Messages_GetAllChats(); // chats = groups/channels (does not include users dialogs)
            Console.WriteLine("This user has joined the following:");
            foreach (var (id, chat) in chats.chats)
            {
                if (excluded.Contains(id))
                {
                    continue;
                }

                if (chat.IsChannel)
                {
                    Channel channel = (Channel)chat;
                    supergroups.Add(
                        new Supergroup
                        {
                            Id = channel.id,
                            Title = channel.title,
                            MainUsername = channel.MainUsername,
                        });
                }
            }


            var dialogs = await _client.Messages_GetAllDialogs();
            dialogs.CollectUsersChats(_users, _chats);
            // long tprogerId = 1044298783;
            // var tProger = _chats[tprogerId];

            await _service.InsertSupergroups(supergroups);

            foreach (var supergroup in supergroups)
            {
                //dialogs = await _client.Messages_GetAllDialogs();
                //dialogs.CollectUsersChats(_users, _chats);
                var peer = _chats[supergroup.Id];
                Console.WriteLine(peer.MainUsername);

                var lastMessage = await _service.GetLastMessage(supergroup.Id);
                var minId = lastMessage?.TelegramId ?? 0;
                await GetAllPeerMessages(peer, minId: minId);

                if (minId > 0)
                {
                    var firstRecordedId = await _service.GetFirstMessage(supergroup.Id);
                    var offset = firstRecordedId?.TelegramId ?? 0;
                    Console.WriteLine(peer.MainUsername);
                    await GetAllPeerMessages(peer, offset);
                }
            }

            IsActive = false;
        }

        private async Task OnUpdate(UpdatesBase updates)
        {
            updates.CollectUsersChats(_users, _chats);
        }

        private async Task GetAllPeerMessages(InputPeer peer, int offset = 0, int minId = 0)
        {
            for (var offsetId = offset;;)
            {
                var messages = await _client.Messages_GetHistory(peer, offsetId, min_id: minId);
                //var ads = _client.Channels_GetMessages();

                if (messages.Messages.Length == 0) break;
                foreach (var msgBase in messages.Messages)
                {
                    var from = messages.UserOrChat(msgBase.From ?? msgBase.Peer); // from can be User/Chat/Channel
                    if (msgBase is TL.Message msg)
                    {
                        if (msg.message.Length == 0)
                        {
                            continue;
                        }

                        if (msg.ID % 100 == 0)
                        {
                            Console.WriteLine(msg.ID);
                        }

                        _outMessages.Enqueue(new Message
                        {
                            Id = 0,
                            TelegramId = msg.id,
                            Sender = peer.ID,
                            Content = msg.message,
                            //TODO: optimize word counting
                            // Words = msg.message.Split(' ').Length
                        });
                    }
                }

                offsetId = messages.Messages[^1].ID;
                await Task.Delay(Random.Shared.Next(100, 1100));
            }
        }

        private void LoadEnvironmentVariables(out int apiId, out string apiHash)
        {
            string? apiIdString = Environment.GetEnvironmentVariable("telegram_meta_dater_api_id");
            if (apiIdString == null)
            {
                throw new Exception("No api id is provided");
            }

            if (!int.TryParse(apiIdString, out apiId))
            {
                throw new Exception("Incorrect api id is provided");
            }

            string? apiHashString = Environment.GetEnvironmentVariable("telegram_meta_dater_api_hash");

            apiHash = apiHashString ?? throw new Exception("No api hash is provided");
        }
    }
}