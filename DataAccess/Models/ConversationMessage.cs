namespace DataAccess.Models;

public class ConversationMessage : AuditableEntity
{
    

    public Guid ConversationId { get; set; }

    public Guid SenderUserId { get; set; }

    public bool IsSystemGenerated { get; set; }

    public string Body { get; set; } = string.Empty;
    public Conversation Conversation { get; set; } = null!;

    public User SenderUser { get; set; } = null!;

    public ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();
}