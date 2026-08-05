using Microsoft.AspNetCore.Identity;
namespace DataAccess.Models;

public class User: IdentityUser<int>
{
   

    public string UserType { get; set; } = string.Empty;

    public int? PrimaryDepartmentId { get; set; }

    public Department? PrimaryDepartment { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<Ticket> SubmittedTickets { get; set; }
        = new List<Ticket>();

    public ICollection<ConversationParticipant> ConversationParticipants { get; set; }
        = new List<ConversationParticipant>();

    public ICollection<ConversationMessage> SentMessages { get; set; }
        = new List<ConversationMessage>();

    public ICollection<TicketComment> TicketComments { get; set; }
        = new List<TicketComment>();

    public ICollection<TicketWatcher> WatchedTickets { get; set; }
        = new List<TicketWatcher>();

    public ICollection<TicketStatusHistory> StatusChanges { get; set; }
        = new List<TicketStatusHistory>();

    public ICollection<TicketFieldHistory> FieldChanges { get; set; }
        = new List<TicketFieldHistory>();

    public ICollection<WorkflowStep> AssignedWorkflowSteps { get; set; }
        = new List<WorkflowStep>();

    public ICollection<WorkflowStepInstance> AssignedStepInstances { get; set; }
        = new List<WorkflowStepInstance>();

    public ICollection<TicketTransfer> SentTransfers { get; set; }
        = new List<TicketTransfer>();

    public ICollection<TicketTransfer> ReceivedTransfers { get; set; }
        = new List<TicketTransfer>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}