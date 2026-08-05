using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public record RegisterRequest(
        [param: Required, EmailAddress, StringLength(160)]
        string Email,

        [param: Required, StringLength(128, MinimumLength = 8,
                    ErrorMessage = "Use at least 8 characters.")]
        string Password,

        [param: Required, StringLength(20)]
        string PhoneNumber);

    public record LoginRequest(
        [param: Required, EmailAddress] string Email,
        [param: Required] string Password);
    
    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresUtc,
        string Email,
        IReadOnlyList<string> Roles);
}