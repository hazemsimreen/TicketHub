using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class CreateDepartmentDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }
}

public class UpdateDepartmentDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }
}

public class DepartmentDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }

    public int CategoryCount { get; set; }

    public int AgentCount { get; set; }

    public int OpenTicketCount { get; set; }
}