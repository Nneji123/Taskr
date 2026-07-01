using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Auth.DTOs;
using API.Features.Auth.Services;

namespace API.Features.Auth.Controllers;

/// <summary>
/// Authentication and account endpoints. Handles registration, login, token
/// rotation, password recovery, and the current-user query.
/// </summary>
[Route("v1/auth")]
[EnableRateLimiting("auth-strict")]
public class AuthController(IAuthService authService, ICurrentUser currentUser) : BaseController(currentUser)
{
    /// <summary>Register a new account.</summary>
    /// <remarks>
    /// Creates a new user with the supplied credentials. Returns the created
    /// user profile. Duplicate email addresses are rejected.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RegisterAsync(request, ip, ct);
        return CreatedResult(result, "Registration successful");
    }

    /// <summary>Authenticate a user and return access + refresh tokens.</summary>
    /// <remarks>
    /// Exchanges an email and password for a short-lived access token and a
    /// long-lived refresh token. Use the access token in the
    /// <c>Authorization: Bearer &lt;token&gt;</c> header on subsequent requests.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.LoginAsync(request, ip, ct);
        return OkResult(result, "Login successful");
    }

    /// <summary>Rotate a refresh token for a new access + refresh pair.</summary>
    /// <remarks>
    /// Submits a valid refresh token to receive a fresh access token and a
    /// newly rotated refresh token. The previous refresh token is invalidated
    /// (with reuse detection).
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ct);
        return OkResult(result, "Token refreshed");
    }

    /// <summary>Get the currently authenticated user.</summary>
    /// <remarks>Returns the profile of the user identified by the bearer token.</remarks>
    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("api-default")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await authService.GetCurrentUserAsync(CurrentUser.Id, ct);
        return OkResult(user);
    }

    /// <summary>Request a password reset OTP via email.</summary>
    /// <remarks>
    /// Sends a one-time code to the supplied email if an account exists.
    /// Always returns 200 to avoid leaking which addresses are registered.
    /// </remarks>
    [HttpPost("password-reset")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetRequest request, CancellationToken ct)
    {
        await authService.PasswordResetAsync(request.Email, ct);
        return OkResult<object?>(null, "If the email exists, a reset code has been sent.");
    }

    /// <summary>Confirm a password reset using the OTP.</summary>
    /// <remarks>
    /// Submits the OTP emailed via <c>POST /v1/auth/password-reset</c> along
    /// with a new password. The OTP is single-use.
    /// </remarks>
    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> PasswordResetConfirm([FromBody] PasswordResetConfirmRequest request, CancellationToken ct)
    {
        await authService.PasswordResetConfirmAsync(request.Email, request.Otp, request.NewPassword, ct);
        return OkResult<object?>(null, "Password has been reset successfully.");
    }

    /// <summary>Change the password of the currently authenticated user.</summary>
    /// <remarks>
    /// Requires the existing password for verification. All refresh tokens for
    /// the user are invalidated after a successful change.
    /// </remarks>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(CurrentUser.Id, request.OldPassword, request.NewPassword, ct);
        return OkResult<object?>(null, "Password changed successfully.");
    }
}
