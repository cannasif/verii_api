using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IVoiceSynthesisService
{
    Task<VoiceSynthesisResultDto> SynthesizeAsync(VoiceSynthesisRequestDto request, CancellationToken cancellationToken = default);
}
