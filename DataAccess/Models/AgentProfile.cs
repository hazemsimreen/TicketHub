namespace DataAccess.Models;

public class AgentProfile : AuditableEntity
{
    public int Id { get; set; }

    public int AgentId { get; set; }

    public int MaxOpenTickets { get; set; } = 10;

    public Agent Agent { get; set; } = null!;
}