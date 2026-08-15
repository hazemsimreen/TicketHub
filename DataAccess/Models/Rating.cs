namespace DataAccess.Models;


/// Allows one rating per resolved ticket.

public class Rating : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    //1..5 — also enforced by a check constraint, see AppDbContext.
    public int Stars { get; set; }

    public string? Comment { get; set; }

    public Guid RatedByUserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User RatedByUser { get; set; } = null!;
}
