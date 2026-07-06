using V3RII.Application.Common;
using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
}
