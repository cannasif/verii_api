namespace V3RII.Infrastructure.Options;

public sealed class NetworkSecurityOptions
{
    public const string SectionName = "NetworkSecurity";

    public bool EnableSwagger { get; set; }
    public bool EnableHangfireDashboard { get; set; }
    public string[] AdminIpAllowList { get; set; } = [];
}
