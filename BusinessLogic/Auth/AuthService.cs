using BusinessLogic.Common;
using Contract.Dtos;
using Contracts.Security;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace BusinessLogic.Auth;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest req, string? ip, string? ua, CancellationToken ct = default);
    Task<ServiceResult<AuthResponse>> RefreshAsync(string rawToken, string? ip, string? ua, CancellationToken ct = default);
    Task<ServiceResult> LogoutAsync(string rawToken, CancellationToken ct = default);
    Task<ServiceResult> LogoutAllAsync(string userId, CancellationToken ct = default);
    Task<ServiceResult<MeResponse>> GetMeAsync(string userId, CancellationToken ct = default);
    Task<ServiceResult<MeResponse>> UpdateMeAsync(string userId, UpdateMeRequest req, CancellationToken ct = default);
    Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordRequest req, CancellationToken ct = default);
    Task<ServiceResult> ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default);
    Task<ServiceResult> ConfirmEmailAsync(ConfirmEmailRequest req, CancellationToken ct = default);
    Task<ServiceResult> ResendConfirmationAsync(string email, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<SessionDto>>> GetSessionsAsync(string userId, CancellationToken ct = default);
    Task<ServiceResult> RevokeSessionAsync(string userId, Guid sessionId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokens,
        IEmailSender email,
        AppDbContext db,
        IConfiguration config)
    {
        _userManager = userManager;
        _tokens = tokens;
        _email = email;
        _db = db;
        _config = config;
    }

    // ── Register ──────────────────────────────────────────────────────────────
    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest req, CancellationToken ct = default)
    {
        var user = new User
        {
            UserName = req.Email,
            Email = req.Email,
            PhoneNumber = req.PhoneNumber,
            UserType = AppRoles.Citizen   // always Citizen — never set from body
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return ServiceResult<AuthResponse>.BadRequest(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        // Assign Citizen role from custom Role table
        var citizenRole = await _db.Set<Role>()
            .FirstOrDefaultAsync(r => r.Code == AppRoles.Citizen, ct);
        if (citizenRole is not null)
        {
            _db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = citizenRole.Id });
            await _db.SaveChangesAsync(ct);
        }

        // Reload user with roles for JWT claims
        var fullUser = await _db.Set<User>()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id, ct);

        var (access, expires) = _tokens.CreateAccessToken(fullUser);
        var raw = await IssueRefreshTokenAsync(user.Id, null, null, ct);
        var roles = fullUser.UserRoles.Select(ur => ur.Role.Code).ToList();

        return ServiceResult<AuthResponse>.Created(
            new AuthResponse(access, raw, expires, user.Email!, roles));
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest req, string? ip, string? ua, CancellationToken ct = default)
    {
        const string generic = "Invalid email or password.";

        var user = await _db.Set<User>()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == req.Email && !u.IsDeleted, ct);

        if (user is null)
            return ServiceResult<AuthResponse>.Unauthorized(generic);

        if (await _userManager.IsLockedOutAsync(user))
            return ServiceResult<AuthResponse>.Unauthorized(
                "Account is locked. Try again later.");

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return ServiceResult<AuthResponse>.Unauthorized(generic);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var (access, expires) = _tokens.CreateAccessToken(user);
        var raw = await IssueRefreshTokenAsync(user.Id, ip, ua, ct);
        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();

        return ServiceResult<AuthResponse>.Success(
            new AuthResponse(access, raw, expires, user.Email!, roles));
    }

    // ── Refresh ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        string rawToken, string? ip, string? ua, CancellationToken ct = default)
    {
        var hash = RefreshToken.Hash(rawToken);
        var token = await _db.Set<RefreshToken>()
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
            return ServiceResult<AuthResponse>.Unauthorized("Invalid token.");

        // Reuse detection — token already used/revoked means possible theft
        if (token.IsUsed || token.IsRevoked)
        {
            await RevokeAllAsync(token.UserId, "Reuse detected", ct);
            return ServiceResult<AuthResponse>.Unauthorized(
                "Token reuse detected. All sessions revoked.");
        }

        if (DateTime.UtcNow > token.ExpiresAt)
            return ServiceResult<AuthResponse>.Unauthorized("Token expired.");

        // Rotate
        token.IsUsed = true;
        var newRaw = await IssueRefreshTokenAsync(token.UserId, ip, ua, ct);

        var (access, expires) = _tokens.CreateAccessToken(token.User);
        var roles = token.User.UserRoles.Select(ur => ur.Role.Code).ToList();

        return ServiceResult<AuthResponse>.Success(
            new AuthResponse(access, newRaw, expires, token.User.Email!, roles));
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    public async Task<ServiceResult> LogoutAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = RefreshToken.Hash(rawToken);
        var token = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is not null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "User logout";
            await _db.SaveChangesAsync(ct);
        }
        return ServiceResult.NoContent();
    }

    // ── Logout All ────────────────────────────────────────────────────────────
    public async Task<ServiceResult> LogoutAllAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            return ServiceResult.BadRequest("Invalid user id.");

        await RevokeAllAsync(uid, "Logout everywhere", ct);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
            await _userManager.UpdateSecurityStampAsync(user);

        return ServiceResult.NoContent();
    }

    // ── Get Me ────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<MeResponse>> GetMeAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await _db.Set<User>()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId && !u.IsDeleted, ct);

        if (user is null) return ServiceResult<MeResponse>.NotFound("User not found.");

        return ServiceResult<MeResponse>.Success(Map(user));
    }

    // ── Update Me ─────────────────────────────────────────────────────────────
    public async Task<ServiceResult<MeResponse>> UpdateMeAsync(
        string userId, UpdateMeRequest req, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted)
            return ServiceResult<MeResponse>.NotFound("User not found.");

        user.PhoneNumber = req.PhoneNumber;
        await _userManager.UpdateAsync(user);
        return await GetMeAsync(userId, ct);
    }

    // ── Change Password ───────────────────────────────────────────────────────
    public async Task<ServiceResult> ChangePasswordAsync(
        string userId, ChangePasswordRequest req, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ServiceResult.NotFound("User not found.");

        var result = await _userManager.ChangePasswordAsync(
            user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return ServiceResult.BadRequest(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        if (Guid.TryParse(userId, out var uid))
            await RevokeAllAsync(uid, "Password changed", ct);

        return ServiceResult.NoContent();
    }

    // ── Forgot Password ───────────────────────────────────────────────────────
    public async Task<ServiceResult> ForgotPasswordAsync(
        string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && !user.IsDeleted)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontend = _config["FrontendUrl"] ?? "http://localhost:3000";
            var link = $"{frontend}/reset-password" +
                           $"?email={Uri.EscapeDataString(user.Email!)}" +
                           $"&token={Uri.EscapeDataString(token)}";
            await _email.SendAsync(user.Email!, "Reset your password", link, ct);
        }
        // ALWAYS 204 — prevents email enumeration
        return ServiceResult.NoContent();
    }

    // ── Reset Password ────────────────────────────────────────────────────────
    public async Task<ServiceResult> ResetPasswordAsync(
        ResetPasswordRequest req, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null) return ServiceResult.BadRequest("Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!result.Succeeded)
            return ServiceResult.BadRequest(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        if (Guid.TryParse(user.Id.ToString(), out var uid))
            await RevokeAllAsync(uid, "Password reset", ct);

        return ServiceResult.NoContent();
    }

    // ── Confirm Email ────────────────────────────────────────────────────────-
    public async Task<ServiceResult> ConfirmEmailAsync(
        ConfirmEmailRequest req, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null) return ServiceResult.BadRequest("Invalid request.");

        var result = await _userManager.ConfirmEmailAsync(user, req.Token);
        if (!result.Succeeded)
            return ServiceResult.BadRequest(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        return ServiceResult.NoContent();
    }

    // ── Resend Confirmation ───────────────────────────────────────────────────
    public async Task<ServiceResult> ResendConfirmationAsync(
        string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && !user.IsDeleted && !user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontend = _config["FrontendUrl"] ?? "http://localhost:3000";
            var link = $"{frontend}/confirm-email" +
                           $"?email={Uri.EscapeDataString(user.Email!)}" +
                           $"&token={Uri.EscapeDataString(token)}";
            await _email.SendAsync(user.Email!, "Confirm your email", link, ct);
        }
        return ServiceResult.NoContent();
    }

    // ── Get Sessions ──────────────────────────────────────────────────────────
    public async Task<ServiceResult<IReadOnlyList<SessionDto>>> GetSessionsAsync(
        string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            return ServiceResult<IReadOnlyList<SessionDto>>.BadRequest("Invalid user id.");

        var now = DateTime.UtcNow;
        var sessions = await _db.Set<RefreshToken>()
            .Where(t => t.UserId == uid && !t.IsRevoked && !t.IsUsed && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionDto(t.Id, t.CreatedByIp, t.UserAgent, t.CreatedAt, t.ExpiresAt))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<SessionDto>>.Success(sessions);
    }

    // ── Revoke Session ────────────────────────────────────────────────────────
    public async Task<ServiceResult> RevokeSessionAsync(
        string userId, Guid sessionId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            return ServiceResult.BadRequest("Invalid user id.");

        var token = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == uid, ct);

        if (token is null) return ServiceResult.NotFound("Session not found.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = "Revoked by user";
        await _db.SaveChangesAsync(ct);

        return ServiceResult.NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> IssueRefreshTokenAsync(
        Guid userId, string? ip, string? ua, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshToken.Hash(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedByIp = ip,
            UserAgent = ua
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    private async Task RevokeAllAsync(Guid userId, string reason, CancellationToken ct)
    {
        var tokens = await _db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed)
            .ToListAsync(ct);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = reason;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static MeResponse Map(User u) => new(
        u.Id.ToString(),
        u.UserName ?? string.Empty,
        u.Email ?? string.Empty,
        u.PhoneNumber,
        u.EmailConfirmed,
        u.UserRoles.Select(ur => ur.Role.Code).ToList(),
        u.PrimaryDepartmentId?.ToString());
}