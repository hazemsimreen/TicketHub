namespace BusinessLogic;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    Guid? DepartmentId { get; }
    Guid? PrimaryDepartmentId => DepartmentId;
    bool IsInRole(string roleCode);
}
