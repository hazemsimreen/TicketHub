using Microsoft.AspNetCore.Identity;

namespace DataAccess.Models;

public class User : IdentityUser<Guid>
{

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public string UserType { get; set; } = string.Empty;

    public int? PrimaryDepartmentId { get; set; }

    public Department? PrimaryDepartment { get; set; }

    // User Roles
    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    // Tickets submitted by this user
    public ICollection<Ticket> SubmittedTickets { get; set; }
        = new List<Ticket>();

    // Conversations
    public ICollection<ConversationParticipant> ConversationParticipants { get; set; }
        = new List<ConversationParticipant>();

    public ICollection<ConversationMessage> SentMessages { get; set; }
        = new List<ConversationMessage>();

    // Ticket comments
    public ICollection<TicketComment> TicketComments { get; set; }
        = new List<TicketComment>();

    // Ticket watchers
    public ICollection<TicketWatcher> WatchedTickets { get; set; }
        = new List<TicketWatcher>();

    // Ticket status history
    public ICollection<TicketStatusHistory> StatusChanges { get; set; }
        = new List<TicketStatusHistory>();

    // Ticket field history
    public ICollection<TicketFieldHistory> FieldChanges { get; set; }
        = new List<TicketFieldHistory>();

    // Workflow
    public ICollection<WorkflowStep> AssignedWorkflowSteps { get; set; }
        = new List<WorkflowStep>();

    public ICollection<WorkflowStepInstance> AssignedStepInstances { get; set; }
        = new List<WorkflowStepInstance>();

    // Ticket transfers
    public ICollection<TicketTransfer> SentTransfers { get; set; }
        = new List<TicketTransfer>();

    public ICollection<TicketTransfer> ReceivedTransfers { get; set; }
        = new List<TicketTransfer>();

    // Notifications
    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();

    public ICollection<Ticket> AssignedTickets { get; set; }
    = new List<Ticket>();

}