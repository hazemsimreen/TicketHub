namespace DataAccess.Models;

public class Attachment: AuditableEntity
{


    public Guid? TicketId { get; set; }

    public Guid? CommentId { get; set; }

    public Guid? MessageId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public Ticket? Ticket { get; set; }

    public TicketComment? Comment { get; set; }

    public ConversationMessage? Message { get; set; }
}