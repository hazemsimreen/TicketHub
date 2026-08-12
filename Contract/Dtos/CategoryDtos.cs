using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class CreateCategoryDto
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int? DefaultPriorityId { get; set; }
}

public class UpdateCategoryDto
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int? DefaultPriorityId { get; set; }

    public bool IsActive { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public int? DefaultPriorityId { get; set; }

    public bool IsActive { get; set; }
}

public class CategoryLookupDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}