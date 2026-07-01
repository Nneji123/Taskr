namespace API.Features.Auth.DTOs;

/// <summary>Payload for <c>POST /v1/auth/register</c>.</summary>
public class RegisterRequest
{
    /// <summary>Unique email address. Used as the login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password. Stored as a bcrypt hash server-side.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Given name shown in the UI.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name shown in the UI.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional avatar image URL. Must be uploaded via <c>POST /v1/files</c>.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Optional free-form key/value metadata stored on the user record.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Payload for <c>POST /v1/auth/login</c>.</summary>
public class LoginRequest
{
    /// <summary>Account email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password in plaintext (sent over TLS).</summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/refresh</c>.</summary>
public class RefreshTokenRequest
{
    /// <summary>The refresh token previously issued by login or a prior refresh call.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/password-reset</c>.</summary>
public class PasswordResetRequest
{
    /// <summary>Email address of the account to recover. Must exist.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/password-reset/confirm</c>.</summary>
public class PasswordResetConfirmRequest
{
    /// <summary>Email address of the account being recovered.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>6-digit one-time code sent to the user's email.</summary>
    public string Otp { get; set; } = string.Empty;

    /// <summary>The new password to set on the account.</summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Payload for <c>POST /v1/auth/change-password</c>.</summary>
public class ChangePasswordRequest
{
    /// <summary>The user's current password, required for verification.</summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>The new password to set on the account.</summary>
    public string NewPassword { get; set; } = string.Empty;
}
