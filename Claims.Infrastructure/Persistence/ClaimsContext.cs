using Claims.Domain;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Persistence;

public class ClaimsContext : DbContext
{
    public DbSet<Claim> Claims { get; init; } = null!;
    public DbSet<Cover> Covers { get; init; } = null!;

    public ClaimsContext(DbContextOptions<ClaimsContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>(builder =>
        {
            builder.ToCollection("claims");
            builder.Property(c => c.Id).HasElementName("_id");
            builder.Property(c => c.CoverId).HasElementName("coverId");
            builder.Property(c => c.Created).HasElementName("created");
            builder.Property(c => c.Name).HasElementName("name");
            builder.Property(c => c.Type).HasElementName("claimType");
            builder.Property(c => c.DamageCost).HasElementName("damageCost");
        });

        modelBuilder.Entity<Cover>(builder =>
        {
            builder.ToCollection("covers");
            builder.Property(c => c.Id).HasElementName("_id");
            builder.Property(c => c.StartDate).HasElementName("startDate");
            builder.Property(c => c.EndDate).HasElementName("endDate");
            builder.Property(c => c.Type).HasElementName("claimType");
            builder.Property(c => c.Premium).HasElementName("premium");
        });
    }
}
