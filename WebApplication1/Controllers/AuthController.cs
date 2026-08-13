using API.Auth;
using BusinessLogic;
using BusinessLogic.Auth;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _me;

    public AuthController(IAuthService auth, ICurrentUser me)
    {
        _auth = auth;
        _me = me;
    }

    // ── Register ──────────────────────────────────────────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(req, ct);
        return r.IsSuccess ? StatusCode(201, r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var r = await _auth.LoginAsync(req, ip, ua, ct);
        return r.IsSuccess ? Ok(r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var r = await _auth.RefreshAsync(req.RefreshToken, ip, ua, ct);
        return r.IsSuccess ? Ok(r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct)
    {
        var r = await _auth.LogoutAsync(req.RefreshToken, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Logout All ────────────────────────────────────────────────────────────
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var r = await _auth.LogoutAllAsync(_me.UserId!, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Get Me ────────────────────────────────────────────────────────────────
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var r = await _auth.GetMeAsync(_me.UserId!, ct);
        return r.IsSuccess ? Ok(r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Update Me ─────────────────────────────────────────────────────────────
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateMeRequest req, CancellationToken ct)
    {
        var r = await _auth.UpdateMeAsync(_me.UserId!, req, ct);
        return r.IsSuccess ? Ok(r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Change Password ───────────────────────────────────────────────────────
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req, CancellationToken ct)
    {
        var r = await _auth.ChangePasswordAsync(_me.UserId!, req, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Forgot Password ───────────────────────────────────────────────────────
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(req.Email, ct);
        return NoContent();
    }

    // ── Reset Password ────────────────────────────────────────────────────────
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        var r = await _auth.ResetPasswordAsync(req, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Confirm Email ─────────────────────────────────────────────────────────
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest req, CancellationToken ct)
    {
        var r = await _auth.ConfirmEmailAsync(req, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Resend Confirmation ───────────────────────────────────────────────────
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.ResendConfirmationAsync(req.Email, ct);
        return NoContent();
    }

    // ── Get Sessions ──────────────────────────────────────────────────────────
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var r = await _auth.GetSessionsAsync(_me.UserId!, ct);
        return r.IsSuccess ? Ok(r.Data) : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }

    // ── Revoke Session ────────────────────────────────────────────────────────
    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        var r = await _auth.RevokeSessionAsync(_me.UserId!, id, ct);
        return r.IsSuccess ? NoContent() : Problem(r.ErrorMessage, statusCode: r.StatusCode);
    }
}