namespace DataAccess.Models;

public class WorkflowDefinitions
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public int? CategoryId { get; set; }

    public int Version { get; set; }

    public bool IsDefault { get; set; }
}