namespace DataAccess.Models;

public class TicketTransfers
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int? OriginStepInstanceId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public int? ToDepartmentId { get; set; }

    public string Status { get; set; } = string.Empty;
}