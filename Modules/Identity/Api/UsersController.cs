using Microsoft.AspNetCore.Mvc;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/users")]
[PermissionAuthorize(PermissionCodes.AdminUsersManage)]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItemDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await userService.GetUsersAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserListItemDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserListItemDto>>> CreateUser(CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await userService.CreateUserAsync(request, cancellationToken);
        return Ok(ApiResponse<UserListItemDto>.Ok(result, "Kullanıcı oluşturuldu."));
    }
}
