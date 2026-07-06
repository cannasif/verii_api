namespace V3RII.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "v3rii-api";
    public string Audience { get; set; } = "v3rii-client";
    public string Secret { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_FOR_PRODUCTION";
    public int ExpireMinutes { get; set; } = 480;
}
