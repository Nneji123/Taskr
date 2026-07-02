using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using API.Data;
using API.Common;
using API.Common.Email;
using API.Common.Files;
using API.Common.Files.Models;
using API.Common.Storage;
using API.Features.Auth.Models;
using API.Features.Auth.DTOs;
using API.Options;

namespace API.Features.Auth.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken ct);
    Task<AuthTokensResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct);
    Task<AuthTokensResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task PasswordResetAsync(string email, CancellationToken ct);
    Task PasswordResetConfirmAsync(string email, string otp, string newPassword, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken ct);
}

public class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IEmailQueue emailQueue,
    ICacheService cache,
    IStorageService storage,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private static readonly TimeSpan AvatarSignedUrlTtl = TimeSpan.FromMinutes(5);

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken ct)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();
        var emailHash = EmailHashHelper.ComputeHash(normalizedEmail);

        if (await db.Users.AnyAsync(u => u.EmailHash == emailHash, ct))
            throw new ConflictException("Email is already registered.");

        var user = new User
        {
            Email = normalizedEmail,
            EmailHash = emailHash,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            AvatarId = request.AvatarId,
            Metadata = request.Metadata ?? new Dictionary<string, object?>()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        emailQueue.Enqueue(new EmailQueueEntry(user.Email, "Welcome to Taskr", FeatureEmailTemplates.Auth.Welcome,
            new() { ["FirstName"] = user.FirstName }));

        return new RegisterResponse { User = await MapUserAsync(user, ct) };
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct)
    {
        var emailHash = EmailHashHelper.ComputeHash(request.Email.ToLowerInvariant().Trim());

        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailHash == emailHash, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is deactivated.");

        user.LastLoginAt = DateTime.UtcNow;
        var tokens = await IssueTokensAsync(user, ct);

        emailQueue.Enqueue(new EmailQueueEntry(user.Email, "New Login Detected", FeatureEmailTemplates.Auth.NewLogin,
            new() { ["FirstName"] = user.FirstName, ["IpAddress"] = ipAddress, ["Time"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }));

        await db.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<AuthTokensResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (stored.RevokedAt != null)
        {
            await RevokeFamilyAsync(stored.UserId, ct);
            throw new UnauthorizedException("Refresh token has been revoked. All sessions invalidated.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = "Rotated";

        var tokens = await IssueTokensAsync(stored.User, ct, tokenHash);
        await db.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);
        return await MapUserAsync(user, ct);
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, CancellationToken ct, string? replacedBy = null)
    {
        var (accessToken, expiresAt) = jwtTokenService.GenerateAccessToken(user);
        var rawRefresh = jwtTokenService.GenerateRefreshToken();
        var refreshHash = jwtTokenService.HashRefreshToken(rawRefresh);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays),
            ReplacedByTokenHash = replacedBy
        });

        return new AuthTokensResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            AccessTokenExpiresAt = expiresAt,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays),
            User = await MapUserAsync(user, ct)
        };
    }

    private async Task RevokeFamilyAsync(Guid userId, CancellationToken ct)
    {
        var active = await db.RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active) { t.RevokedAt = DateTime.UtcNow; t.RevokedReason = "Family revoked"; }
    }

    public async Task PasswordResetAsync(string email, CancellationToken ct)
    {
        var emailHash = EmailHashHelper.ComputeHash(email.ToLowerInvariant().Trim());
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailHash == emailHash, ct);
        if (user is null) return;

        var otp = string.Concat(Guid.NewGuid().ToString("N")[..6].Select(c => (char)('0' + (c % 10))));
        var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otp);
        await cache.SetAsync($"otp:password_reset:{emailHash}", hashedOtp, TimeSpan.FromMinutes(10), ct);

        emailQueue.Enqueue(new EmailQueueEntry(user.Email, "Password Reset Request", FeatureEmailTemplates.Auth.PasswordReset,
            new() { ["Otp"] = otp }));
    }

    public async Task PasswordResetConfirmAsync(string email, string otp, string newPassword, CancellationToken ct)
    {
        var emailHash = EmailHashHelper.ComputeHash(email.ToLowerInvariant().Trim());
        var key = $"otp:password_reset:{emailHash}";
        var hashedOtp = await cache.GetAsync<string>(key, ct);
        if (hashedOtp is null || !BCrypt.Net.BCrypt.Verify(otp, hashedOtp))
            throw new UnauthorizedException("Invalid or expired OTP.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailHash == emailHash, ct)
            ?? throw new NotFoundException("User", email);

        user.PasswordHash = passwordHasher.Hash(newPassword);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(key, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!passwordHasher.Verify(oldPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(newPassword);
        await db.SaveChangesAsync(ct);
    }

    private async Task<UserResponse> MapUserAsync(User user, CancellationToken ct)
    {
        FileResponse? avatar = null;
        if (user.AvatarId.HasValue)
        {
            var record = await db.FileRecords.FirstOrDefaultAsync(f => f.Id == user.AvatarId.Value, ct);
            if (record is not null)
                avatar = MapFile(record);
        }

        return new UserResponse
        {
            Id = user.Id, Email = user.Email, FirstName = user.FirstName,
            LastName = user.LastName, Avatar = avatar,
            CreatedAt = user.CreatedAt, UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt, Metadata = user.Metadata
        };
    }

    private FileResponse MapFile(FileRecord record) => new()
    {
        Id = record.Id,
        Key = record.Key,
        Url = storage.GetSignedUrl(record.Key, AvatarSignedUrlTtl),
        OriginalFilename = record.OriginalFilename,
        FileSize = record.FileSize,
        ContentType = record.ContentType,
        CreatedAt = record.CreatedAt
    };
}
