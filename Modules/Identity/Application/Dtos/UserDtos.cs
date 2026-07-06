namespace V3RII.Application.DTOs;

public sealed record UserListItemDto(long Id, string Email, string FullName, bool IsActive);
public sealed record CreateUserRequestDto(string Email, string FullName, string Password, IReadOnlyCollection<string> PermissionCodes);
