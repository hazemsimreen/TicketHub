namespace DataAccess.Models;

public class TicketStatusHistory : AuditableEntity
{
    public Guid TicketId { get; set; }

    public Guid? FromStatusId { get; set; }

    public Guid ToStatusId { get; set; }

    public Guid ChangedByUserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public TicketStatus? FromStatus { get; set; }

    public TicketStatus ToStatus { get; set; } = null!;

    public User ChangedByUser { get; set; } = null!;
}