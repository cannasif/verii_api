namespace V3RII.Application.DTOs;

public sealed record VoiceSynthesisRequestDto(string Text, string? Language, string? Persona);

public sealed record VoiceSynthesisResultDto(
    bool Enabled,
    string? AudioBase64,
    string ContentType,
    string Provider,
    string VoiceName);
