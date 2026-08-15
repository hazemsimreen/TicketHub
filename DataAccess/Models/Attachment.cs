namespace DataAccess.Models;

public class Attachment : AuditableEntity
{

    public Guid Id { get; set; }
    public Guid? TicketId { get; set; }

    public Guid? CommentId { get; set; }

    public Guid? MessageId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// Role 4 addition — needed to set Content-Type on download.
    public string ContentType { get; set; } = string.Empty;

    /// Role 4 addition.
    public long SizeBytes { get; set; }

    /// Role 4 addition — identity from the token at upload time.
    public Guid UploadedByUserId { get; set; }

    public Ticket? Ticket { get; set; }

    public TicketComment? Comment { get; set; }

    public ConversationMessage? Message { get; set; }

    public User UploadedByUser { get; set; } = null!;
}
