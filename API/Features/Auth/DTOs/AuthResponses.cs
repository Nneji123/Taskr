using API.Common.Files;

namespace API.Features.Auth.DTOs;

/// <summary>Public-facing user profile returned by auth and user endpoints.</summary>
public class UserResponse
{
    /// <summary>Unique user identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Account email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Given name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional avatar file, resolved with a fresh signed URL.</summary>
    public FileResponse? Avatar { get; set; }

    /// <summary>Timestamp (UTC) the user record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp (UTC) the user record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Timestamp (UTC) of the user's most recent successful login, if any.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Free-form key/value metadata attached to the user record.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>Access + refresh token pair returned by login and refresh endpoints.</summary>
public class AuthTokensResponse
{
    /// <summary>Short-lived JWT used in the <c>Authorization</c> header.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Long-lived token used to obtain new access tokens via the refresh endpoint.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Timestamp (UTC) at which the access token expires.</summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>Timestamp (UTC) at which the refresh token expires.</summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    /// <summary>The authenticated user's profile.</summary>
    public UserResponse User { get; set; } = null!;
}

/// <summary>Response envelope for <c>POST /v1/auth/register</c>.</summary>
public class RegisterResponse
{
    /// <summary>The newly created user profile.</summary>
    public UserResponse User { get; set; } = null!;
}
