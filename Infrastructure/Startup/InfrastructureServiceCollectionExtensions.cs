using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.Options;
using V3RII.Infrastructure.Persistence;
using V3RII.Infrastructure.Services;

namespace V3RII.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(options =>
        {
            var section = configuration.GetSection(JwtOptions.SectionName);
            options.Issuer = section[nameof(JwtOptions.Issuer)] ?? options.Issuer;
            options.Audience = section[nameof(JwtOptions.Audience)] ?? options.Audience;
            options.Secret = section[nameof(JwtOptions.Secret)] ?? options.Secret;
            if (int.TryParse(section[nameof(JwtOptions.ExpireMinutes)], out var expireMinutes))
            {
                options.ExpireMinutes = expireMinutes;
            }
        });
        services.Configure<LlmOptions>(options =>
        {
            var section = configuration.GetSection(LlmOptions.SectionName);
            options.Enabled = bool.TryParse(section[nameof(LlmOptions.Enabled)], out var enabled) && enabled;
            options.ApiKey = section[nameof(LlmOptions.ApiKey)] ?? options.ApiKey;
            options.Model = section[nameof(LlmOptions.Model)] ?? options.Model;
        });
        services.Configure<MailOptions>(options =>
        {
            var section = configuration.GetSection(MailOptions.SectionName);
            options.Enabled = bool.TryParse(section[nameof(MailOptions.Enabled)], out var enabled) && enabled;
            options.Host = section[nameof(MailOptions.Host)] ?? options.Host;
            if (int.TryParse(section[nameof(MailOptions.Port)], out var port))
            {
                options.Port = port;
            }
            options.EnableSsl = !bool.TryParse(section[nameof(MailOptions.EnableSsl)], out var ssl) || ssl;
            options.UserName = section[nameof(MailOptions.UserName)] ?? options.UserName;
            options.Password = section[nameof(MailOptions.Password)] ?? options.Password;
            options.From = section[nameof(MailOptions.From)] ?? options.From;
        });
        services.Configure<NetworkSecurityOptions>(options =>
        {
            var section = configuration.GetSection(NetworkSecurityOptions.SectionName);
            options.EnableSwagger = bool.TryParse(section[nameof(NetworkSecurityOptions.EnableSwagger)], out var swagger) && swagger;
            options.EnableHangfireDashboard = bool.TryParse(section[nameof(NetworkSecurityOptions.EnableHangfireDashboard)], out var hangfire) && hangfire;
            options.AdminIpAllowList = section.GetSection(nameof(NetworkSecurityOptions.AdminIpAllowList)).Get<string[]>() ?? [];
        });
        services.AddDbContext<V3RiiDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddHttpClient();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IChatAnswerService, ChatAnswerService>();
        services.AddScoped<IMailOutboxProcessor, MailOutboxProcessor>();

        return services;
    }
}
