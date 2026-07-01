using API.Common;
using API.Features.Projects.Models;
using API.Features.Tasks.Models;

namespace API.Features.Auth.Models;

public class User : BaseModel
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    [EncryptedPersonalData]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<TaskItem> AssignedTasks { get; set; } = [];
}
