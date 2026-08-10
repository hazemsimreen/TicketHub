namespace DataAccess.Models;

public class TicketFieldHistory : AuditableEntity
{

    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public Guid ChangedByUserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User ChangedByUser { get; set; } = null!;
}