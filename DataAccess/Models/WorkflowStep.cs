namespace DataAccess.Models;

public class WorkflowStep
{
    public int Id { get; set; }

    public int WorkflowDefinitionId { get; set; }

    public int StepOrder { get; set; }

    public int? RoleId { get; set; }

    public int? AssignedUserId { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public Role? Role { get; set; }

    public User? AssignedUser { get; set; }

    public ICollection<WorkflowStepInstance> StepInstances { get; set; }
        = new List<WorkflowStepInstance>();
}