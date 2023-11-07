using DatabaseService;
using TelegremService;
using TelegremService.Index;


//using var client = new WTelegram.Client(Config);
//var myself = await client.LoginUserIfNeeded();
//Console.WriteLine($"We are logged-in as {myself} (id {myself.id})");
string? number = Environment.GetEnvironmentVariable("telegram_meta_dater_debug_number");

if (number == null)
{
    Console.WriteLine("No number");
    return;
}

using var service = new Service();
using var telegramClient = new TelegramClient(number, "1", service);
await telegramClient.Login();


var task = Task.Run(async () => await telegramClient.RunSth());

//while (telegramClient.IsActive)
//{
//    // var messages = new List<Message>();
//    while (telegramClient.TryGetMessage(out var message))
//    {
//        await service.InsertMessage(message);
//        await IndexMessage(message.Sender, message.TelegramId);
//    }
//    await Task.WhenAll(tasks);
//    tasks.Clear();
//    //await Task.Delay(5000);
//}

var tasks = new List<Task>();

for (int i = 0; i < 2; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        using var innerService = new Service();
        var indexer = new Indexer(innerService);
        while (telegramClient.IsActive)
        {
            while (telegramClient.TryGetMessage(out var message))
            {
                var index = indexer.IndexMessage(message);
                message.Words = index.wordsCount;
                await innerService.InsertMessage(message);

                var dbMessage = await innerService.GetMessageBySenderAndTelegramId(message.Sender, message.TelegramId);
                if (dbMessage == null)
                {
                    continue;
                }
                await indexer.LoadIndexIntoDb(dbMessage.Id, index);
            }

            await Task.Delay(5000);
        }
    }));
}

await Task.WhenAll(tasks);
task.Wait();
