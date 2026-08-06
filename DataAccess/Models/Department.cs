namespace DataAccess.Models;

public class Department : AuditableEntity
{

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }

    public ICollection<Department> ChildDepartments { get; set; }
        = new List<Department>();

    public ICollection<User> Users { get; set; }
        = new List<User>();

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();

    public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; }
        = new List<WorkflowDefinition>();

    public ICollection<TicketTransfer> ReceivedTransfers { get; set; }
        = new List<TicketTransfer>();
}