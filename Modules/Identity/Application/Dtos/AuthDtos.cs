namespace V3RII.Application.DTOs;

public sealed record LoginRequestDto(string Email, string Password);
public sealed record AuthResponseDto(string Token, DateTimeOffset ExpiresAt, CurrentUserDto User);
public sealed record CurrentUserDto(long Id, string Email, string FullName, IReadOnlyCollection<string> Permissions);
