namespace DatabaseService.Data
{
    public class WordMessage
    {
        public long Id { get; set; }
        public long MessageId { get; set; }
        public int WordId { get; set; }
        public int Count { get; set; }
        public float TermFrequency { get; set; }
    }
}