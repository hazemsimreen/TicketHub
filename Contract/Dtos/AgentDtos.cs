using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class CreateAgentDto
{
    public Guid UserId { get; set; }

    public Guid DepartmentId { get; set; }

    public List<string> SkillNames { get; set; }
        = new List<string>();
}

public class UpdateAgentDto
{
    public Guid DepartmentId { get; set; }

    public List<string> SkillNames { get; set; }
        = new List<string>();
}

public class UpdateAgentProfileDto
{
    [Range(1, 100)]
    public int MaxOpenTickets { get; set; }
}

public class AgentProfileDto
{
    public Guid Id { get; set; }

    public int MaxOpenTickets { get; set; }
}

public class AgentDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int CurrentOpenTicketCount { get; set; }

    public AgentProfileDto? Profile { get; set; }

    public List<string> Skills { get; set; }
        = new List<string>();
}