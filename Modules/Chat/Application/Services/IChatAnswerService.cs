using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IChatAnswerService
{
    Task<ChatAnswerDto> AnswerAsync(ChatAnswerRequestDto request, CancellationToken cancellationToken = default);
}
