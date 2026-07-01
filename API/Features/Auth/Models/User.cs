using API.Common;
using API.Features.Projects.Models;
using API.Features.Tasks.Models;

namespace API.Features.Auth.Models;

/// <summary>
/// Application user account. Owner of projects and assignee of tasks.
/// All persisted user records share this shape.
/// </summary>
public class User : BaseModel
{
    /// <summary>Unique email address. Used as the login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Bcrypt hash of the user's password. Never returned in API responses.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Given name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional avatar image URL.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Phone number, stored encrypted at rest.</summary>
    [EncryptedPersonalData]
    public string? PhoneNumber { get; set; }

    /// <summary>Whether the account is active. Inactive accounts cannot authenticate.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether the user has confirmed ownership of their email address.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>Timestamp (UTC) of the user's most recent successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Active refresh tokens issued to this user.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>Projects owned by this user.</summary>
    public ICollection<Project> Projects { get; set; } = [];

    /// <summary>Tasks assigned to this user.</summary>
    public ICollection<TaskItem> AssignedTasks { get; set; } = [];
}
