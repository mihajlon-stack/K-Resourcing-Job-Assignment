using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Claims.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can construct <see cref="AuditContext"/>
/// without spinning up the host's Testcontainers-backed SQL Server. The connection string here
/// is never used at runtime — the real one comes from Testcontainers via AddInfrastructure.
/// </summary>
public class AuditContextFactory : IDesignTimeDbContextFactory<AuditContext>
{
    public AuditContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AuditContext(optionsBuilder.Options);
    }
}
