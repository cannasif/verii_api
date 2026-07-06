using System.Security.Claims;
using V3RII.Application.Common.Abstractions;
using V3RII.Application.Common.Security;

namespace V3RII.Api.Authorization;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public long? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimConstants.UserId);
            return long.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Permissions => httpContextAccessor.HttpContext?.User
        .FindAll(ClaimConstants.Permission)
        .Select(x => x.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();
}
