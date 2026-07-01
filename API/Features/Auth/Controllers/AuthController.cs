using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using API.Common;
using API.Features.Auth.DTOs;
using API.Features.Auth.Services;

namespace API.Features.Auth.Controllers;

[Route("v1/auth")]
[EnableRateLimiting("auth-strict")]
public class AuthController(IAuthService authService, ICurrentUser currentUser) : BaseController(currentUser)
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RegisterAsync(request, ip, ct);
        return CreatedResult(result, "Registration successful");
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.LoginAsync(request, ip, ct);
        return OkResult(result, "Login successful");
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ct);
        return OkResult(result, "Token refreshed");
    }

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("api-default")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await authService.GetCurrentUserAsync(CurrentUser.Id, ct);
        return OkResult(user);
    }

    [HttpPost("password-reset")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetRequest request, CancellationToken ct)
    {
        await authService.PasswordResetAsync(request.Email, ct);
        return OkResult<object?>(null, "If the email exists, a reset code has been sent.");
    }

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PasswordResetConfirm([FromBody] PasswordResetConfirmRequest request, CancellationToken ct)
    {
        await authService.PasswordResetConfirmAsync(request.Email, request.Otp, request.NewPassword, ct);
        return OkResult<object?>(null, "Password has been reset successfully.");
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("write-strict")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(CurrentUser.Id, request.OldPassword, request.NewPassword, ct);
        return OkResult<object?>(null, "Password changed successfully.");
    }
}
