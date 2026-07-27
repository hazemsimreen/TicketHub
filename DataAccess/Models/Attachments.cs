namespace DataAccess.Models;

public class Attachments
{
    public int Id { get; set; }

    public int? TicketId { get; set; }

    public int? CommentId { get; set; }

    public int? MessageId { get; set; }

    public string StorageKey { get; set; } = string.Empty;
}