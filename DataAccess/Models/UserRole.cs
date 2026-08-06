namespace DataAccess.Models;

public class UserRole : AuditableEntity
{
   

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? DepartmentId { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public Department? Department { get; set; }
}