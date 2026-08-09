using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public Guid? DepartmentId { get; set; }

    public List<string> Roles { get; set; }
        = new List<string>();
}

public class UpdateUserDto
{
    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public Guid? DepartmentId { get; set; }

    public List<string> Roles { get; set; }
        = new List<string>();
}

public class ResetUserPasswordDto
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public Guid? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; }

    public List<string> Roles { get; set; }
        = new List<string>();
}

public class UserLookupDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}