using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class CreateSkillDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateSkillDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class SkillDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AgentCount { get; set; }
}