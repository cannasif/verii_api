namespace V3RII.Domain.Entities;

public sealed class UserPermissionGroup
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long PermissionGroupId { get; set; }
    public PermissionGroup PermissionGroup { get; set; } = null!;
}
