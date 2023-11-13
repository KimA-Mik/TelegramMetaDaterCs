using DatabaseService;
using DatabaseService.Data;

namespace TelegramService.Index;

public class Indexer
{
    private readonly Lexer _lexer = new Lexer();
    private readonly Service _service;

    public Indexer(Service? service = null)
    {
        _service = service ?? new Service();
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

    public async Task LoadIndexIntoDb(long messageId, MessageIndex index)
    {
        //TODO: Optimize db operations
        var wms = new List<WordMessage>();
        var words = index.tfIndex.Keys;
        await _service.InsertWords(words);
        var wordsEntities = await _service.GetWordsByStrings(words);

        foreach (var wordEntity in wordsEntities)
        {
            var count = index.tfIndex[wordEntity.Text];
            wms.Add(new WordMessage
            {
                Id = 0,
                MessageId = messageId,
                Count = count,
                WordId = wordEntity.Id,
                TermFrequency = (float)count / index.wordsCount
            });
        }

        await _service.InsertWordMessages(wms);
    }

    private Dictionary<string, int> IndexTf(string text, out int wordsCount)
    {
        var startTime = DateTime.Now;

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