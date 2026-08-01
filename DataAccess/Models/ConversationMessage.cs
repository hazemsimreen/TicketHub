namespace DataAccess.Models;

public class ConversationMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public bool IsSystemGenerated { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public User SenderUser { get; set; } = null!;

    public ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();
}