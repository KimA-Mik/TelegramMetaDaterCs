using DatabaseService;
using TelegramService;

var service = new Service();
var searcher = new Searcher(service);
while (true)
{
    Console.Write("\nQuery:");
    var query = Console.ReadLine();
    if (query == null)
    {
        continue;
    }

    var resultMessages = await searcher.Search(query, 2);

    foreach (var message in resultMessages)
    {
        Console.WriteLine(message.Content);
    }
}