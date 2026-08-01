namespace DataAccess.Models;

public class Ticket
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SubmittedByUserId { get; set; }

    public int CategoryId { get; set; }

    public int DepartmentId { get; set; }

    public int PriorityId { get; set; }

    public int StatusId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public User SubmittedByUser { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public TicketPriority Priority { get; set; } = null!;

    public TicketStatus Status { get; set; } = null!;

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