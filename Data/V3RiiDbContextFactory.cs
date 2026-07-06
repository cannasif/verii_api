using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace V3RII.Infrastructure.Persistence;

public sealed class V3RiiDbContextFactory : IDesignTimeDbContextFactory<V3RiiDbContext>
{
    public V3RiiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<V3RiiDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=V3RII;Trusted_Connection=True;TrustServerCertificate=True");
        return new V3RiiDbContext(optionsBuilder.Options);
    }
}
