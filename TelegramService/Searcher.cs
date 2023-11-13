using DatabaseService;
using DatabaseService.Data;
using TelegramService.Index;

namespace TelegramService
{
    public class Searcher
    {
        private readonly Service _service;
        private readonly Lexer _lexer;

        public Searcher(Service? service = null)
        {
            _service = _service == null ? new Service() : service!;
            _lexer = new Lexer();
        }

        public async Task<List<Message>> Search(string query, int limit = 25)
        {
            var startTime = DateTime.Now;

            var resultMap = new Dictionary<long, double>();
            var result = new List<Message>();

            var messageCount = await _service.GetMessagesCount();
            foreach (var token in _lexer.Tokenize(query))
            {
                var wms = await _service.GetWmsForWord(token);
                foreach (var wm in wms)
                {
                    var tf = wm.TermFrequency;
                    var idf = Math.Log(messageCount / (1f + wms.Count)) + 1;
                    if (resultMap.ContainsKey(wm.MessageId))
                    {
                        resultMap[wm.MessageId] += tf * idf;
                    }
                    else
                    {
                        resultMap[wm.MessageId] = tf * idf;
                    }
                }
            }

            var sortedKv = from index in resultMap orderby index.Value descending select index;
            var messages = await _service.GetMessagesByIds(resultMap.Keys);

            foreach (var keyValue in sortedKv.Take(limit))
            {
                Console.WriteLine($"{keyValue.Value} - {messages.Find(it => it.Id == keyValue.Key).Content}\n");
            }

            Console.WriteLine("================================================================");


            //return sorted.Take(limit).ToList;
            var sorted = messages.OrderBy(i => -resultMap[i.Id]);

            var ts = DateTime.Now.Subtract(startTime);
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

            Console.WriteLine($"[Info] Query: {query} executed in {elapsedTime}");


            return sorted.Take(limit).ToList();
        }
    }
}