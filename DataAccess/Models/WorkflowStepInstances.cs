namespace DataAccess.Models;

public class WorkflowStepInstances
{
    public int Id { get; set; }

    public int WorkflowInstanceId { get; set; }

    public int WorkflowStepId { get; set; }

    public int StepOrder { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? AssignedToUserId { get; set; }
}