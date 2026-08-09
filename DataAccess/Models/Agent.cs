namespace DataAccess.Models;

public class Agent : AuditableEntity
{
    public Guid UserId { get; set; }

    public Guid DepartmentId { get; set; }

    public User User { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public AgentProfile? Profile { get; set; }

    public ICollection<Skill> Skills { get; set; }
        = new List<Skill>();
}