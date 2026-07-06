using V3RII.Domain.Common;

namespace V3RII.Domain.Entities;

public sealed class PermissionDefinition : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
