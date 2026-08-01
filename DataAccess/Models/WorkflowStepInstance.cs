namespace DataAccess.Models;

public class WorkflowStepInstance
{
    public int Id { get; set; }

    public int WorkflowInstanceId { get; set; }

    public int WorkflowStepId { get; set; }

    public int StepOrder { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? AssignedToUserId { get; set; }

    public WorkflowInstance WorkflowInstance { get; set; } = null!;

    public WorkflowStep WorkflowStep { get; set; } = null!;

    public User? AssignedToUser { get; set; }

    public ICollection<TicketComment> Comments { get; set; }
        = new List<TicketComment>();

    public ICollection<TicketTransfer> OriginTransfers { get; set; }
        = new List<TicketTransfer>();
}