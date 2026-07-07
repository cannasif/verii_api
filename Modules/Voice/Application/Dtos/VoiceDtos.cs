namespace V3RII.Application.DTOs;

public sealed record VoiceSynthesisRequestDto(string Text, string? Language, string? Persona);

public sealed record VoiceSynthesisResultDto(
    bool Enabled,
    string? AudioBase64,
    string ContentType,
    string Provider,
    string VoiceName);

public sealed record VoiceRealtimeSessionRequestDto(string? Language, string? Persona, string? SessionId);

public sealed record VoiceRealtimeSessionResultDto(
    bool Enabled,
    string? ClientSecret,
    string Model,
    string Voice,
    string Endpoint,
    DateTimeOffset? ExpiresAt);
