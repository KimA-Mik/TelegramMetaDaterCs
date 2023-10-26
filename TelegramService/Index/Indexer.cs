using DatabaseService;
using DatabaseService.Data;

namespace TelegremService.Index;

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
        var words = 0;
        var tfIndex = IndexTf(message.content, out words);

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
        await _service.InsertWors(words);

        foreach (var word in words)
        {
            var wordEntity = await _service.GetWordByWord(word);
            wms.Add(new WordMessage
            {
                id = 0,
                messageId = messageId,
                count = index.tfIndex[word],
                wordId = wordEntity!.id
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
                index[token] = 0;
            }
        }

        return index;
    }
}