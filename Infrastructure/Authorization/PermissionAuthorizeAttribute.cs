using Microsoft.AspNetCore.Authorization;

namespace V3RII.Api.Authorization;

public sealed class PermissionAuthorizeAttribute(string permission) : AuthorizeAttribute(permission);
