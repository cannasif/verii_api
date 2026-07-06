using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using V3RII.Application.Common.Abstractions;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class AuthService(
    V3RiiDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    ICurrentUserContext currentUserContext) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .Include(x => x.PermissionGroups)
                .ThenInclude(x => x.PermissionGroup)
                    .ThenInclude(x => x.Permissions)
                        .ThenInclude(x => x.PermissionDefinition)
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && x.IsActive, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("E-posta veya parola hatalı.");
        }

        var permissions = user.PermissionGroups
            .SelectMany(x => x.PermissionGroup.Permissions.Select(p => p.PermissionDefinition.Code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
        var token = CreateToken(user.Id, user.Email, user.FullName, permissions, expiresAt);
        var currentUser = new CurrentUserDto(user.Id, user.Email, user.FullName, permissions);

        return new AuthResponseDto(token, expiresAt, currentUser);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserContext.UserId is null)
        {
            throw new UnauthorizedAccessException("Oturum bulunamadı.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserContext.UserId.Value && x.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");
        }

        return new CurrentUserDto(user.Id, user.Email, user.FullName, currentUserContext.Permissions);
    }

    private string CreateToken(long userId, string email, string fullName, IReadOnlyCollection<string> permissions, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimConstants.UserId, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Name, fullName)
        };

        claims.AddRange(permissions.Select(permission => new Claim(ClaimConstants.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
