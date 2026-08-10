namespace DataAccess.Models;

public class Skill : AuditableEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Agent> Agents { get; set; }
        = new List<Agent>();
}