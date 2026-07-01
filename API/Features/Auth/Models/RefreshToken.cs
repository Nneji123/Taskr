using API.Common;

namespace API.Features.Auth.Models;

public class RefreshToken : BaseModel
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevokedReason { get; set; }
}
