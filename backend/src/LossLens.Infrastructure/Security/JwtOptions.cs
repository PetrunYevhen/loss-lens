namespace LossLens.Infrastructure.Security;

/// <summary>Settings of the access tokens. The key comes from the environment (Jwt__Key), never from the repository.</summary>
public sealed class JwtOptions
{
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "LossLens";
    public string Audience { get; set; } = "LossLens.Web";

    /// <summary>Secret for signing tokens (HMAC SHA-256), at least 32 bytes.</summary>
    public string Key { get; set; } = "";

    public int LifetimeMinutes { get; set; } = 60;
}
