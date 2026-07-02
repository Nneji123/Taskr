using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Features.Auth.DTOs;

/// <summary>Payload for <c>POST /v1/auth/register</c>.</summary>
public class RegisterRequest
{
    /// <summary>Unique email address. Used as the login identifier.</summary>
    [Required]
    [EmailAddress]
    [StringLength(254, MinimumLength = 3)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password. Stored as a bcrypt hash server-side.</summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Given name shown in the UI.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name shown in the UI.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional avatar file id. Upload via <c>POST /v1/files</c> first, then pass the returned <c>id</c>.</summary>
    [JsonPropertyName("avatarId")]
    public Guid? AvatarId { get; set; }

    /// <summary>Optional free-form key/value metadata stored on the user record.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Payload for <c>POST /v1/auth/login</c>.</summary>
public class LoginRequest
{
    /// <summary>Account email address.</summary>
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password in plaintext (sent over TLS).</summary>
    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/refresh</c>.</summary>
public class RefreshTokenRequest
{
    /// <summary>The refresh token previously issued by login or a prior refresh call.</summary>
    [Required]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/password-reset</c>.</summary>
public class PasswordResetRequest
{
    /// <summary>Email address of the account to recover. Must exist.</summary>
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/password-reset/confirm</c>.</summary>
public class PasswordResetConfirmRequest
{
    /// <summary>Email address of the account being recovered.</summary>
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>6-digit one-time code sent to the user's email.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    [JsonPropertyName("otp")]
    public string Otp { get; set; } = string.Empty;

    /// <summary>The new password to set on the account.</summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/change-password</c>.</summary>
public class ChangePasswordRequest
{
    /// <summary>The user's current password, required for verification.</summary>
    [Required]
    [JsonPropertyName("oldPassword")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>The new password to set on the account.</summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}
