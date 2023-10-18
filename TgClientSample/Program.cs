using TgClientSample;

//using var client = new WTelegram.Client(Config);
//var myself = await client.LoginUserIfNeeded();
//Console.WriteLine($"We are logged-in as {myself} (id {myself.id})");
string? number = Environment.GetEnvironmentVariable("telegram_meta_dater_debug_number");

if (number == null)
{
    Console.WriteLine("No number");
    return;
}


using var telegramClient = new TelegramClient(number, "1");
await telegramClient.Login();
await telegramClient.RunSth();

