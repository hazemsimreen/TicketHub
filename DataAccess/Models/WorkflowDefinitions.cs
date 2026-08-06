namespace DataAccess.Models;

public class WorkflowDefinition : AuditableEntity
{


    public string Name { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public Guid? CategoryId { get; set; }

    public int Version { get; set; }

    public bool IsDefault { get; set; }

    public Department Department { get; set; } = null!;

    public Category? Category { get; set; }

    public ICollection<WorkflowStep> Steps { get; set; }
        = new List<WorkflowStep>();

    public ICollection<WorkflowInstance> Instances { get; set; }
        = new List<WorkflowInstance>();
}