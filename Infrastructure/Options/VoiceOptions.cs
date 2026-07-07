namespace V3RII.Infrastructure.Options;

public sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "AzureSpeech";
    public string AzureSpeechKey { get; set; } = string.Empty;
    public string AzureSpeechRegion { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "audio-24khz-48kbitrate-mono-mp3";
    public string ContentType { get; set; } = "audio/mpeg";
    public string TurkishFemaleVoice { get; set; } = "tr-TR-EmelNeural";
    public string TurkishMaleVoice { get; set; } = "tr-TR-AhmetNeural";
    public string EnglishFemaleVoice { get; set; } = "en-US-JennyNeural";
    public string EnglishMaleVoice { get; set; } = "en-US-GuyNeural";
    public bool RealtimeEnabled { get; set; }
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string RealtimeModel { get; set; } = "gpt-realtime-mini";
    public string RealtimeFemaleVoice { get; set; } = "marin";
    public string RealtimeMaleVoice { get; set; } = "cedar";
    public int RealtimeClientSecretTtlSeconds { get; set; } = 300;
}
