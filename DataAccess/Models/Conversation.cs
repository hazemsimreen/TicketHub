namespace DataAccess.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        public Ticket Ticket { get; set; } = null!;
        public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    }
}
