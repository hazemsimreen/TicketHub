using System.Security.Cryptography;

namespace DataAccess.Models;

/// <summary>
/// Represents a hashed refresh token stored in the database.
/// The raw token is NEVER persisted — only its SHA-256 hash.
/// </summary>
public class RefreshToken
{
    public Guid     Id               { get; set; } = Guid.NewGuid();
    public Guid     UserId           { get; set; }

    /// <summary>SHA-256 hex digest of the raw 64-byte token sent to the client.</summary>
    public string   TokenHash        { get; set; } = string.Empty;

    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt        { get; set; }
    public string?  CreatedByIp      { get; set; }
    public string?  UserAgent        { get; set; }

    // Revocation
    public bool     IsRevoked        { get; set; }
    public DateTime? RevokedAt       { get; set; }
    public string?  RevokedReason    { get; set; }

    // Rotation chain — which token replaced this one
    public Guid?    ReplacedByTokenId { get; set; }

    // Single-use: set true once exchanged for a new pair
    public bool     IsUsed           { get; set; }

    // Navigation
    public User     User             { get; set; } = null!;

    /// <summary>Active when not revoked, not used, and not yet expired.</summary>
    public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow < ExpiresAt;

    /// <summary>Compute SHA-256 hex of the raw token string.</summary>
    public static string Hash(string raw)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
