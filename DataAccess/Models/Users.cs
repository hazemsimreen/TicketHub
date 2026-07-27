namespace DataAccess.Models;

public class Users
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public int? PrimaryDepartmentId { get; set; }

    public Departments? PrimaryDepartment { get; set; }
}