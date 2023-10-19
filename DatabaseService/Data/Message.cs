namespace DatabaseService.Data
{
    public class Message
    {
        public int id { get; set; }
        public int telegramId { get; set; }
        public long sender { get; set; }
        public string content { get; set; }
    }
}