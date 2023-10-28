namespace DatabaseService.Data
{
    public class Message
    {
        public int Id { get; set; }
        public int TelegramId { get; set; }
        public long Sender { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}