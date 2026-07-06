namespace V3RII.Application.Common.Security;

public static class PermissionCodes
{
    public const string SupportTicketsRead = "support.tickets.read";
    public const string SupportTicketsManage = "support.tickets.manage";
    public const string KnowledgeRead = "support.knowledge.read";
    public const string KnowledgeManage = "support.knowledge.manage";
    public const string AnalyticsRead = "support.analytics.read";
    public const string AdminUsersManage = "admin.users.manage";
    public const string HangfireRead = "system.hangfire.read";

    public static readonly string[] All =
    [
        SupportTicketsRead,
        SupportTicketsManage,
        KnowledgeRead,
        KnowledgeManage,
        AnalyticsRead,
        AdminUsersManage,
        HangfireRead
    ];
}
