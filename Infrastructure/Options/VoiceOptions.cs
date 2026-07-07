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
    public bool TranscriptionEnabled { get; set; }
    public string TranscriptionProvider { get; set; } = "LocalProcess";
    public string TranscriptionExecutablePath { get; set; } = string.Empty;
    public string TranscriptionArgumentsTemplate { get; set; } = "--language {language} --file \"{input}\"";
    public int TranscriptionTimeoutSeconds { get; set; } = 45;
    public int TranscriptionMaxFileBytes { get; set; } = 8 * 1024 * 1024;
}
