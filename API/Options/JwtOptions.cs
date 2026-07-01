namespace API.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "mercadotnet";
    public string Audience { get; set; } = "mercadotnet-clients";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
