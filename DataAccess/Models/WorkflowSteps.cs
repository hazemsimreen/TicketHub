namespace DataAccess.Models;

public class WorkflowSteps
{
    public int Id { get; set; }

    public int WorkflowDefinitionId { get; set; }

    public int StepOrder { get; set; }

    public int? RoleId { get; set; }

    public int? AssignedUserId { get; set; }
}