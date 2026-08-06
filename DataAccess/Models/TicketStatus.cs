namespace DataAccess.Models;

public class TicketStatus : AuditableEntity
{

    public string Code { get; set; } = string.Empty;

    public bool IsTerminal { get; set; }

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();

    public ICollection<TicketStatusHistory> FromStatusHistories { get; set; }
        = new List<TicketStatusHistory>();

    public ICollection<TicketStatusHistory> ToStatusHistories { get; set; }
        = new List<TicketStatusHistory>();
}