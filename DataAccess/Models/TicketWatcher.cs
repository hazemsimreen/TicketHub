namespace DataAccess.Models;

public class TicketWatcher
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int UserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User User { get; set; } = null!;
}