using System.Security.Claims;
using BusinessLogic;
using Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace API.Auth;

/// <summary>
/// Reads the current user's identity and roles from the JWT claims
/// stored in HttpContext.User by the JwtBearerHandler.
/// 
/// Note: ASP.NET Core's JsonWebTokenHandler (default in .NET 8+) maps the JWT
/// "sub" claim → ClaimTypes.NameIdentifier regardless of DefaultInboundClaimTypeMap.
/// So we always read NameIdentifier first as the canonical user ID source.
/// </summary>
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    // "sub" gets mapped to ClaimTypes.NameIdentifier by JsonWebTokenHandler.
    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public string? UserName =>
        Principal?.FindFirstValue("name")
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Email =>
        Principal?.FindFirstValue("email")
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? DepartmentId =>
        Guid.TryParse(
            Principal?.FindFirstValue("dept"), out var id)
                ? id : null;

    public IReadOnlyList<string> Roles =>
        Principal?.Claims
        .Where(c => c.Type == ClaimTypes.Role
                 || c.Type == AppClaimTypes.Role   // "role"
                 || c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
        .Select(c => c.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList()
        ?? (IReadOnlyList<string>)[];

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
