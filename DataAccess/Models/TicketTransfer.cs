namespace DataAccess.Models;

public class TicketTransfer
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int OriginStepInstanceId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public int? ToDepartmentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public Ticket Ticket { get; set; } = null!;

    public WorkflowStepInstance OriginStepInstance { get; set; } = null!;

    public User? FromUser { get; set; }

    public User? ToUser { get; set; }

    public Department? ToDepartment { get; set; }
}