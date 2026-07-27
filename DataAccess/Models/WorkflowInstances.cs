namespace DataAccess.Models;

public class WorkflowInstances
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int WorkflowDefinitionId { get; set; }

    public string Status { get; set; } = string.Empty;
}