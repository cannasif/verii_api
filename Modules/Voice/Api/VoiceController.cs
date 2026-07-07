using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/voice")]
public sealed class VoiceController(
    IVoiceSynthesisService voiceSynthesisService,
    IVoiceTranscriptionService voiceTranscriptionService) : ControllerBase
{
    [HttpPost("synthesize")]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<VoiceSynthesisResultDto>>> Synthesize(VoiceSynthesisRequestDto request, CancellationToken cancellationToken)
    {
        var result = await voiceSynthesisService.SynthesizeAsync(request, cancellationToken);
        return Ok(ApiResponse<VoiceSynthesisResultDto>.Ok(result));
    }

    [HttpPost("transcribe")]
    [EnableRateLimiting("public-chatbot")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<VoiceTranscriptionResultDto>>> Transcribe(
        IFormFile audio,
        [FromForm] string? language,
        CancellationToken cancellationToken)
    {
        var result = await voiceTranscriptionService.TranscribeAsync(audio, language, cancellationToken);
        return Ok(ApiResponse<VoiceTranscriptionResultDto>.Ok(result));
    }
}
