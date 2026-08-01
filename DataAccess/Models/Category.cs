namespace DataAccess.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int? DefaultPriorityId { get; set; }

    public Department Department { get; set; } = null!;

    public TicketPriority? DefaultPriority { get; set; }

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();

    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; }
        = new List<WorkflowDefinition>();
}