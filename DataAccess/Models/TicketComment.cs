namespace DataAccess.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int AuthorUserId { get; set; }

    public int? StepInstanceId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User AuthorUser { get; set; } = null!;

    public WorkflowStepInstance? StepInstance { get; set; }

    public TicketComment? ParentComment { get; set; }

    public ICollection<TicketComment> Replies { get; set; }
        = new List<TicketComment>();

    public ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();
}