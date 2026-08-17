using Claims.Application.Auditing;
using Claims.Application.Repositories;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Claims.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqlConnectionString,
        string mongoConnectionString,
        string mongoDatabaseName)
    {
        services.AddDbContext<AuditContext>(options =>
            options.UseSqlServer(sqlConnectionString));

        services.AddDbContext<ClaimsContext>(options =>
        {
            var client = new MongoClient(mongoConnectionString);
            var database = client.GetDatabase(mongoDatabaseName);
            options.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
        });

        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<ICoverRepository, CoverRepository>();

        services.AddOptions<AuditQueueOptions>();
        services.AddSingleton<ChannelAuditQueue>();
        services.AddSingleton<IAuditQueue>(sp => sp.GetRequiredService<ChannelAuditQueue>());
        services.AddHostedService<AuditBackgroundService>();

        return services;
    }
}
