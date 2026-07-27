namespace DataAccess.Models;

public class Tickets
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public int SubmittedByUserId { get; set; }

    public int CategoryId { get; set; }

    public int? DepartmentId { get; set; }

    public int PriorityId { get; set; }

    public int StatusId { get; set; }

    public bool IsDeleted { get; set; }
}