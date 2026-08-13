namespace BusinessLogic;

public interface ICurrentUser
{

    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }

    int? DepartmentId { get; }
    int? PrimaryDepartmentId => DepartmentId;

    bool IsInRole(string roleCode);

}
