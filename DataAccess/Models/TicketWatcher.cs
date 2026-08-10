namespace DataAccess.Models;

public class TicketWatcher : AuditableEntity
{

    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    public Guid UserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User User { get; set; } = null!;
}