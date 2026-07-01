using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using API.Common;
using API.Features.Auth.Models;
using API.Options;

namespace API.Features.Auth.Services;

public interface IJwtTokenService
{
    (string accessToken, DateTime expiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    ClaimsPrincipal? ValidateAccessToken(string token);
}

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public (string accessToken, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer, audience: _jwt.Audience,
            claims: claims, expires: expiresAt, signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = _jwt.Issuer,
                ValidateAudience = true, ValidAudience = _jwt.Audience,
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = key, ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);
        }
        catch { return null; }
    }
}
