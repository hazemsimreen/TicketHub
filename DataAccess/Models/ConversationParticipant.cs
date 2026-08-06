namespace DataAccess.Models;

public class ConversationParticipant : AuditableEntity
{
    

    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public User User { get; set; } = null!;
}