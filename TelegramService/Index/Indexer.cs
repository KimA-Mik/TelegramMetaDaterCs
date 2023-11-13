using DatabaseService;
using DatabaseService.Data;

namespace TelegramService.Index;

public class Indexer
{
    private readonly Lexer _lexer = new Lexer();
    private readonly Service _service;

    public Indexer(Service service)
    {
        _service = service;
    }

    public MessageIndex IndexMessage(Message message)
    {
        var tfIndex = IndexTf(message.Content, out var words);

        return new MessageIndex()
        {
            wordsCount = words,
            tfIndex = tfIndex
        };
    }

    public async Task LoadIndexIntoDb(int messageId, MessageIndex index)
    {
        //TODO: Optimize db operations
        var wms = new List<WordMessage>();
        var words = index.tfIndex.Keys;
        await _service.InsertWords(words);
        var wordsEntities = await _service.GetWordsByStrings(words);

        foreach (var wordEntity in wordsEntities)
        {
            wms.Add(new WordMessage
            {
                Id = 0,
                MessageId = messageId,
                Count = index.tfIndex[wordEntity.Text],
                WordId = wordEntity.Id
            });
        }

        await _service.InsertWordMessages(wms);
    }

    private Dictionary<string, int> IndexTf(string text, out int wordsCount)
    {
        wordsCount = 0;
        var index = new Dictionary<string, int>();
        foreach (var token in _lexer.Tokenize(text))
        {
            wordsCount++;
            if (index.ContainsKey(token))
            {
                index[token] += 1;
            }
            else
            {
                index[token] = 1;
            }
        }

        return index;
    }
}