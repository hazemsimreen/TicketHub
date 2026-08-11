using System.Security.Claims;
using BusinessLogic;
using Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace API.Auth;

/// <summary>
/// Reads the current user's identity and roles from HttpContext JWT claims.
/// </summary>
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public string? UserId =>
        Principal?.FindFirstValue(AppClaimTypes.UserId)
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        Principal?.FindFirstValue(AppClaimTypes.Name)
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Email =>
        Principal?.FindFirstValue(AppClaimTypes.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? DepartmentId =>
        Guid.TryParse(
            Principal?.FindFirstValue(AppClaimTypes.DepartmentId)
            ?? Principal?.FindFirstValue("dept"), out var id)
                ? id : null;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(AppClaimTypes.Role).Select(c => c.Value)
        .Concat(Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList()
        ?? (IReadOnlyList<string>)[];

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
