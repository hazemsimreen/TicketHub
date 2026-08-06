namespace DataAccess.Models;

public class TicketTransfer : AuditableEntity
{
   
    public Guid TicketId { get; set; }

    public Guid OriginStepInstanceId { get; set; }

    public Guid? FromUserId { get; set; }

    public Guid? ToUserId { get; set; }

    public Guid? ToDepartmentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public Ticket Ticket { get; set; } = null!;

    public WorkflowStepInstance OriginStepInstance { get; set; } = null!;

    public User? FromUser { get; set; }

    public User? ToUser { get; set; }

    public Department? ToDepartment { get; set; }
}