namespace DataAccess.Models;

public class AgentProfile : AuditableEntity
{
    public Guid AgentId { get; set; }

    public int MaxOpenTickets { get; set; } = 10;

    public Agent Agent { get; set; } = null!;
}