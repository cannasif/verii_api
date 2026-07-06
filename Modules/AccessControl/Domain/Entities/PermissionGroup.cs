using V3RII.Domain.Common;

namespace V3RII.Domain.Entities;

public sealed class PermissionGroup : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public ICollection<PermissionGroupPermissionDefinition> Permissions { get; set; } = new List<PermissionGroupPermissionDefinition>();
    public ICollection<UserPermissionGroup> Users { get; set; } = new List<UserPermissionGroup>();
}
