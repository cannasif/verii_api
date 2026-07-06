using Hangfire.Dashboard;
using V3RII.Application.Common.Security;

namespace V3RII.Api.Authorization;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.HasClaim(ClaimConstants.Permission, PermissionCodes.HangfireRead);
    }
}
