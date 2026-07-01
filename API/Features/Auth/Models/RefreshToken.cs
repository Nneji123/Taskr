using API.Common;

namespace API.Features.Auth.Models;

/// <summary>
/// Server-side record of an issued refresh token. The plaintext token is never
/// stored; only a hash is persisted. Tokens are rotated on use and revoked
/// when reuse is detected.
/// </summary>
public class RefreshToken : BaseModel
{
    /// <summary>SHA-256 hash of the issued token. The plaintext is only ever held by the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Identifier of the owning user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation property for the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Timestamp (UTC) at which the token expires and is no longer accepted.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Timestamp (UTC) at which the token was revoked, if applicable.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Hash of the refresh token that replaced this one after a successful rotation.</summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>Free-form reason the token was revoked (e.g. "rotated", "reused", "logout", "password_change").</summary>
    public string? RevokedReason { get; set; }
}
