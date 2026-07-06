using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.SqlServer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Abstractions;
using V3RII.Application.Common.Security;
using V3RII.Application.Validators;
using V3RII.Application.Interfaces;
using V3RII.Infrastructure.DependencyInjection;
using V3RII.Infrastructure.Options;
using V3RII.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var hangfireEnabled = bool.TryParse(builder.Configuration["Jobs:EnableHangfire"], out var configuredHangfire) && configuredHangfire;

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCodes.All)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim(ClaimConstants.Permission, permission));
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public-chatbot", limiter =>
    {
        limiter.PermitLimit = 12;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

if (hangfireEnabled)
{
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
    builder.Services.AddHangfireServer();
}

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "V3RII API",
        Version = "v1",
        Description = "Chatbot, destek talebi, bilgi tabanı, yetki ve analitik API'si."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
var networkSecurity = builder.Configuration.GetSection(NetworkSecurityOptions.SectionName).Get<NetworkSecurityOptions>() ?? new NetworkSecurityOptions();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var response = ApiResponse.Fail(exception?.Message ?? "Beklenmeyen hata oluştu.");
        await context.Response.WriteAsJsonAsync(response);
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.Use(async (context, next) =>
{
    if (networkSecurity.AdminIpAllowList.Length > 0 && IsProtectedOperationsPath(context.Request))
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (remoteIp is null || !networkSecurity.AdminIpAllowList.Contains(remoteIp, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
    }

    await next();
});

if (networkSecurity.EnableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "verii-api",
    time = DateTimeOffset.UtcNow
}));

if (hangfireEnabled && networkSecurity.EnableHangfireDashboard)
{
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/hangfire"),
        branch => branch.Use(async (context, next) =>
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            var allowList = networkSecurity.AdminIpAllowList;
            if (allowList.Length > 0 && (remoteIp is null || !allowList.Contains(remoteIp, StringComparer.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            await next();
        }));

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DashboardTitle = "V3RII Jobs"
    });
}

app.MapControllers();
if (hangfireEnabled)
{
    RecurringJob.AddOrUpdate<IMailOutboxProcessor>("mail-outbox", processor => processor.ProcessPendingAsync(CancellationToken.None), Cron.Minutely);
}

var autoMigrate = bool.TryParse(builder.Configuration["Database:AutoMigrate"], out var configuredAutoMigrate) && configuredAutoMigrate;
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<V3RiiDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

static bool IsProtectedOperationsPath(HttpRequest request)
{
    if (request.Path.StartsWithSegments("/hangfire"))
    {
        return true;
    }

    if (request.Path.StartsWithSegments("/api/users") ||
        request.Path.StartsWithSegments("/api/analytics/summary"))
    {
        return true;
    }

    if (request.Path.StartsWithSegments("/api/support/tickets") && request.Method != HttpMethods.Post)
    {
        return true;
    }

    if (request.Path.StartsWithSegments("/api/knowledge") && request.Method != HttpMethods.Get)
    {
        return true;
    }

    return false;
}
