namespace DataAccess.Models;

public class Role
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsDepartmentScoped { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<WorkflowStep> WorkflowSteps { get; set; }
        = new List<WorkflowStep>();
}