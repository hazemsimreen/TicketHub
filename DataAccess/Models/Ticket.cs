namespace DataAccess.Models;

public class Ticket : AuditableEntity
{
    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public Guid SubmittedByUserId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid StatusId { get; set; }

    public Guid? AssignedAgentId { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public User SubmittedByUser { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public TicketPriority Priority { get; set; } = null!;

    public TicketStatus Status { get; set; } = null!;

    public Agent? AssignedAgent { get; set; }

    public Conversation? Conversation { get; set; }

    public ICollection<TicketComment> Comments { get; set; }
        = new List<TicketComment>();

    public ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();

    public ICollection<TicketWatcher> Watchers { get; set; }
        = new List<TicketWatcher>();

    public ICollection<TicketStatusHistory> StatusHistory { get; set; }
        = new List<TicketStatusHistory>();

    public ICollection<TicketFieldHistory> FieldHistory { get; set; }
        = new List<TicketFieldHistory>();

    public ICollection<WorkflowInstance> WorkflowInstances { get; set; }
        = new List<WorkflowInstance>();

    public ICollection<TicketTransfer> Transfers { get; set; }
        = new List<TicketTransfer>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}