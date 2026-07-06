namespace V3RII.Infrastructure.Options;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@v3rii.com";
}
