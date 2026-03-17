namespace Shared.Infrastructure.Bootstrapping;

/// <summary>
/// JWT options bound from configuration (e.g. "JwtSettings" section).
/// Used by AddJwtAuthentication in Bootstrapping.
/// </summary>
public class JwtBearerConfigurationOptions
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 0;
}
