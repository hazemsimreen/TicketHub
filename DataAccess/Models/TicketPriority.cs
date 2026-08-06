namespace DataAccess.Models;

public class TicketPriority : AuditableEntity
{


    public string Code { get; set; } = string.Empty;

    public Guid SortOrder { get; set; }

    public ICollection<Category> DefaultCategories { get; set; }
        = new List<Category>();

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();
}