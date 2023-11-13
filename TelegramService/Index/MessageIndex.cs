namespace TelegramService.Index;

public class MessageIndex
{
    public int wordsCount { get; set; }
    public Dictionary<string, int> tfIndex { get; set; }
}