namespace DataAccess.Models;

public class WorkflowInstance
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int WorkflowDefinitionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public Ticket Ticket { get; set; } = null!;

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public ICollection<WorkflowStepInstance> StepInstances { get; set; }
        = new List<WorkflowStepInstance>();
}