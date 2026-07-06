namespace V3RII.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-mini";
}
