using System.Security.Claims;
using Contracts.Security;
using Microsoft.AspNetCore.Authentication;

namespace API.Auth;

/// <summary>
/// يحوّل الـ claims المطولة (بعد الـ mapping التلقائي لـ JsonWebTokenHandler)
/// إلى الصيغة القصيرة الأصلية المستخدمة بالتوكن (AppClaimTypes)،
/// حتى تشتغل RoleClaimType / User.IsInRole() بشكل صحيح
/// دون الحاجة لتعديل TokenService أو إعدادات الـ mapping العامة.
/// </summary>
public class RoleClaimNormalizationTransformer : IClaimsTransformation
{
    // خريطة: الصيغة الطويلة (اللي بترجع من JsonWebTokenHandler) -> الصيغة القصيرة (AppClaimTypes)
    private static readonly Dictionary<string, string> ClaimTypeMap = new()
    {
        [ClaimTypes.NameIdentifier] = AppClaimTypes.UserId,
        [ClaimTypes.Email] = AppClaimTypes.Email,
        [ClaimTypes.Name] = AppClaimTypes.Name,
        [ClaimTypes.Role] = AppClaimTypes.Role,
    };

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return Task.FromResult(principal);

        foreach (var (longType, shortType) in ClaimTypeMap)
        {
            // لو الصيغة القصيرة موجودة أصلاً، ما في داعي نضيف شي (تفادي تكرار)
            if (identity.HasClaim(c => c.Type == shortType))
                continue;

            var longClaims = identity.FindAll(longType).ToList();

            foreach (var claim in longClaims)
            {
                identity.AddClaim(new Claim(shortType, claim.Value));
            }
        }

        return Task.FromResult(principal);
    }
}