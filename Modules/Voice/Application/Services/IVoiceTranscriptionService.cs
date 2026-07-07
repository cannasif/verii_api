using Microsoft.AspNetCore.Http;
using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IVoiceTranscriptionService
{
    Task<VoiceTranscriptionResultDto> TranscribeAsync(IFormFile audio, string? language, CancellationToken cancellationToken = default);
}
