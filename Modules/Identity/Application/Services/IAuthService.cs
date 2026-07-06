using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
