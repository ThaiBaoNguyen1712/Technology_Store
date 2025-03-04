namespace Tech_Store.Models.DTO.Chat
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
