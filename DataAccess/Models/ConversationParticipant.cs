namespace DataAccess.Models
{
    public class ConversationParticipant
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public int UserId { get; set; }

        public DateTime? LastReadAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
