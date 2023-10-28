using DatabaseService.Data;
using TelegremService.Index;
using TL;
using WTelegram;
using Message = DatabaseService.Data.Message;

namespace TelegremService
{
    public class TelegramClient : IDisposable
    {
        private readonly string _phoneNuber;
        private readonly string _sessionFilePath;
        private readonly WTelegram.Client _client;
        private readonly DatabaseService.Service _service = new DatabaseService.Service();
        private readonly Indexer _indexer;


        private Dictionary<long, User> _users = new();
        private Dictionary<long, ChatBase> _chats = new();


        public TelegramClient(string phoneNumber, string sessionName)
        {
            _phoneNuber = phoneNumber;
            var sessionFileName = sessionName + ".session";
            var currentDirectory = Directory.GetCurrentDirectory();
            var sessionsDirectory = Path.Combine(currentDirectory, "sessions");
            Directory.CreateDirectory(sessionsDirectory);
            _sessionFilePath = Path.Combine(sessionsDirectory, sessionFileName);

            LoadEvrironmentVariables(out var apiId, out var apiHash);

            _client = new WTelegram.Client(apiId, apiHash);
            _indexer = new Indexer(_service);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task Login()
        {
            var loginInfo = _phoneNuber;
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
                switch (chat)
                {
                    case Chat smallgroup when smallgroup.IsActive:
                        Console.WriteLine(
                            $"{id}:  Small group: {smallgroup.title} with {smallgroup.participants_count} members");
                        break;
                    case Channel channel when channel.IsChannel:
                        Console.WriteLine($"{id}: Channel {channel.username}: {channel.title}");
                        Console.WriteLine($"              → access_hash = {channel.access_hash:X}");
                        Console.WriteLine($"              → url = {channel.MainUsername:X}");
                        supergroups.Add(
                            new Supergroup
                            {
                                Id = channel.id,
                                Title = channel.title,
                                MainUsername = channel.MainUsername,
                            });
                        break;
                    case Channel group: // no broadcast flag => it's a big group, also called supergroup or megagroup
                        Console.WriteLine($"{id}: Group {group.username}: {group.title}");
                        //Console.WriteLine($"              → access_hash = {group.access_hash:X}");
                        break;
                }


            var dialogs = await _client.Messages_GetAllDialogs();
            dialogs.CollectUsersChats(_users, _chats);

            long tprogerId = 1044298783;
            var tProger = _chats[tprogerId];

            await _service.InsertSupergroups(supergroups);
            await AddPeerMessagesToDb(tProger);
        }

        private async Task OnUpdate(UpdatesBase updates)
        {
            updates.CollectUsersChats(_users, _chats);
        }

        private async Task<IEnumerable<Message>> GetAllPeerMessages(InputPeer peer)
        {
            var result = new List<Message>();
            for (int offsetId = 0;;)
            {
                var messages = await _client.Messages_GetHistory(peer, offsetId);
                if (messages.Messages.Length == 0) break;
                foreach (var msgBase in messages.Messages)
                {
                    var from = messages.UserOrChat(msgBase.From ?? msgBase.Peer); // from can be User/Chat/Channel
                    if (msgBase is TL.Message msg)
                    {
                        result.Add(new Message
                        {
                            Id = 0,
                            TelegramId = msg.id,
                            Sender = peer.ID,
                            Content = msg.message
                        });
                    }
                }

                offsetId = messages.Messages[^1].ID;
            }

            return result;
        }

        //TODO: Extract indexing from client
        private async Task IndexMessage(long sender, int telegramId)
        {
            var message = await _service.GetMessageBySenderAndTelegramId(sender, telegramId);
            if (message == null)
            {
                return;
            }

            var index = _indexer.IndexMessage(message);
            await _indexer.LoadIndexIntoDb(message.Id, index);
        }

        private async Task AddPeerMessagesToDb(InputPeer peer)
        {
            var result = new List<Message>();
            for (int offsetId = 0;;)
            {
                var messages = await _client.Messages_GetHistory(peer, offsetId);
                if (messages.Messages.Length == 0) break;
                foreach (var msgBase in messages.Messages)
                {
                    var from = messages.UserOrChat(msgBase.From ?? msgBase.Peer); // from can be User/Chat/Channel
                    if (msgBase is TL.Message msg)
                    {
                        result.Add(new Message
                        {
                            Id = 0,
                            TelegramId = msg.id,
                            Sender = peer.ID,
                            Content = msg.message
                        });
                    }
                }

                offsetId = messages.Messages[^1].ID;
                await _service.InsertMessages(result);
                foreach (var message in result)
                {
                    await IndexMessage(message.Sender, message.TelegramId);
                }

                result.Clear();
                await Task.Delay(Random.Shared.Next(100, 2000));
            }
        }


        private void LoadEvrironmentVariables(out int api_id, out string api_hash)
        {
            string? api_id_str = Environment.GetEnvironmentVariable("telegram_meta_dater_api_id");
            if (api_id_str == null)
            {
                throw new Exception("No api id is provided");
            }

            if (!int.TryParse(api_id_str, out api_id))
            {
                throw new Exception("Incorrect api id is provided");
            }

            string? api_hash_str = Environment.GetEnvironmentVariable("telegram_meta_dater_api_hash");
            if (api_hash_str == null)
            {
                throw new Exception("No api hash is provided");
            }

            api_hash = api_hash_str;
        }
    }
}