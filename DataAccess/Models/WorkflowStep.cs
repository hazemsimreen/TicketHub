namespace DataAccess.Models;

public class WorkflowStep : AuditableEntity
{

    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }

    public int StepOrder { get; set; }

    public int? RoleId { get; set; }

    public Guid? AssignedUserId { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public Role? Role { get; set; }

    public User? AssignedUser { get; set; }

    public ICollection<WorkflowStepInstance> StepInstances { get; set; }
        = new List<WorkflowStepInstance>();
}