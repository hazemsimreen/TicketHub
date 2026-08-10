namespace DataAccess.Models;

public class WorkflowInstance : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    public Guid WorkflowDefinitionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public Ticket Ticket { get; set; } = null!;

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public ICollection<WorkflowStepInstance> StepInstances { get; set; }
        = new List<WorkflowStepInstance>();
}