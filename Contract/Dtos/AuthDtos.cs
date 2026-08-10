using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

// ── Register & Login ──────────────────────────────────────────────────────────
public record RegisterRequest(
    [param: Required, EmailAddress, StringLength(160)]
    string Email,

    [param: Required, StringLength(128, MinimumLength = 8,
                ErrorMessage = "Password must be at least 8 characters.")]
    string Password,

    [param: Required, StringLength(20)]
    string PhoneNumber);

public record LoginRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required]               string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresUtc,
    string Email,
    IReadOnlyList<string> Roles);

public record RefreshRequest(
    [param: Required] string RefreshToken);

// ── Profile ───────────────────────────────────────────────────────────────────
public record MeResponse(
    string  Id,
    string  UserName,
    string  Email,
    string? PhoneNumber,
    bool    EmailConfirmed,
    IReadOnlyList<string> Roles,
    string? DepartmentId);

public record UpdateMeRequest(
    [param: StringLength(100)] string? DisplayName,
    [param: Phone]             string? PhoneNumber);

// ── Password ──────────────────────────────────────────────────────────────────
public record ChangePasswordRequest(
    [param: Required] string CurrentPassword,
    [param: Required, StringLength(128, MinimumLength = 8)] string NewPassword);

public record ForgotPasswordRequest(
    [param: Required, EmailAddress] string Email);

public record ResetPasswordRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required]               string Token,
    [param: Required, StringLength(128, MinimumLength = 8)] string NewPassword);

// ── Email Confirmation ────────────────────────────────────────────────────────
public record ConfirmEmailRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required]               string Token);

// ── Sessions ──────────────────────────────────────────────────────────────────
public record SessionDto(
    Guid    Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt,
    DateTime ExpiresAt);