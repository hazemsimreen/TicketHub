using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLogic.Auth;
using DataAccess.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Auth;

public class TokenService : ITokenService
{
    private readonly JwtOptions _opt;
    private readonly SigningCredentials _credentials;

    public TokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key))
        {
            KeyId = "tickethub-2026-01"
        };

        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTime ExpiresUtc) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),

            new("stamp", user.SecurityStamp ?? string.Empty)
        };

        foreach (var userRole in user.UserRoles)
        {
            if (userRole.Role != null)
            {
                claims.Add(new Claim("role", userRole.Role.Code));
            }
        }

        if (user.PrimaryDepartmentId is not null)
            claims.Add(new Claim("dept", user.PrimaryDepartmentId.ToString()!));

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}