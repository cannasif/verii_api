using Microsoft.EntityFrameworkCore;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class UserService(V3RiiDbContext dbContext) : IUserService
{
    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItemDto(x.Id, x.Email, x.FullName, x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Bu e-posta ile kullanıcı zaten var.");
        }

        var permissionCodes = request.PermissionCodes
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissionDefinitions = await dbContext.PermissionDefinitions
            .Where(x => permissionCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        var group = new PermissionGroup
        {
            Name = $"{request.FullName} Permissions",
            NormalizedName = $"{normalizedEmail} PERMISSIONS",
            IsSystem = false,
            Permissions = permissionDefinitions.Select(x => new PermissionGroupPermissionDefinition
            {
                PermissionDefinition = x
            }).ToList()
        };

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PermissionGroups = new List<UserPermissionGroup>
            {
                new() { PermissionGroup = group }
            }
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserListItemDto(user.Id, user.Email, user.FullName, user.IsActive);
    }
}
