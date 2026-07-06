using V3RII.Domain.Common;

namespace V3RII.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<UserPermissionGroup> PermissionGroups { get; set; } = new List<UserPermissionGroup>();
}
