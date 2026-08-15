namespace DataAccess.Models;

public class TicketComment : AuditableEntity
{

    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    public Guid AuthorUserId { get; set; }
    /// Saves the author's name when the comment is created.
    
    public string AuthorNameSnapshot { get; set; } = string.Empty;

    public Guid? StepInstanceId { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }



    public Ticket Ticket { get; set; } = null!;

    public User AuthorUser { get; set; } = null!;

    public WorkflowStepInstance? StepInstance { get; set; }

    public TicketComment? ParentComment { get; set; }

    public ICollection<TicketComment> Replies { get; set; }
        = new List<TicketComment>();

    public ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();
}
