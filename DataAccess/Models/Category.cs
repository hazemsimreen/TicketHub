namespace DataAccess.Models;

public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Guid DepartmentId { get; set; }

    public Guid? DefaultPriorityId { get; set; }

    public Department Department { get; set; } = null!;

    public TicketPriority? DefaultPriority { get; set; }

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();

    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; }
        = new List<WorkflowDefinition>();
}