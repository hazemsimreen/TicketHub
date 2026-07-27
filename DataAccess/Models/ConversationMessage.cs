namespace DataAccess.Models
{
    public class ConversationMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public int SenderUserId { get; set; }

        public bool IsSystemGenerated { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User SenderUser { get; set; } = null!;
    }
}
