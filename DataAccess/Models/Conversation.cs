namespace DataAccess.Models;

public class Conversation : AuditableEntity
{
    

    public Guid TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public ICollection<ConversationParticipant> Participants { get; set; }
        = new List<ConversationParticipant>();

    public ICollection<ConversationMessage> Messages { get; set; }
        = new List<ConversationMessage>();
}