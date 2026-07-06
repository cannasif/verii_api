using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace V3RII.Infrastructure.Persistence;

public sealed class V3RiiDbContextFactory : IDesignTimeDbContextFactory<V3RiiDbContext>
{
    public V3RiiDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=V3RII;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<V3RiiDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new V3RiiDbContext(optionsBuilder.Options);
    }
}
