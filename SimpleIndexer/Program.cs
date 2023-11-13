using DatabaseService;
using TelegramService;
using TelegramService.Index;

string? number = Environment.GetEnvironmentVariable("telegram_meta_dater_debug_number");

if (number == null)
{
    Console.WriteLine("No number");
    return;
}

using var telegramClient = new TelegramClient(number, "1");
await telegramClient.Login();
var task = Task.Run(async () => await telegramClient.RunSth());


var service = new Service();
var indexer = new Indexer(service);
while (telegramClient.IsActive)
{
    while (telegramClient.TryGetMessage(out var message))
    {
        var index = indexer.IndexMessage(message);
        message.Words = index.wordsCount;
        await service.InsertMessage(message);

        var dbMessage = await service.GetMessageBySenderAndTelegramId(message.Sender, message.TelegramId);
        if (dbMessage == null)
        {
            continue;
        }

        await indexer.LoadIndexIntoDb(dbMessage.Id, index);
    }

    await Task.Delay(5000);
}

task.Wait();