namespace V3RII.Domain.Entities;

public sealed class PermissionGroupPermissionDefinition
{
    public long PermissionGroupId { get; set; }
    public PermissionGroup PermissionGroup { get; set; } = null!;
    public long PermissionDefinitionId { get; set; }
    public PermissionDefinition PermissionDefinition { get; set; } = null!;
}
