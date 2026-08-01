namespace DataAccess.Models;

public class TicketFieldHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public int ChangedByUserId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public User ChangedByUser { get; set; } = null!;
}