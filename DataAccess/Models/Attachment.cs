namespace DataAccess.Models;

public class Attachment
{
    public int Id { get; set; }

    public int? TicketId { get; set; }

    public int? CommentId { get; set; }

    public int? MessageId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public Ticket? Ticket { get; set; }

    public TicketComment? Comment { get; set; }

    public ConversationMessage? Message { get; set; }
}