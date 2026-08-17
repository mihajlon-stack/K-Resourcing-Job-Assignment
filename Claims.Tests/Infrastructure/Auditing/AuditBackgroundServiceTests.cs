using Claims.Application.Auditing;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Claims.Tests.Infrastructure.Auditing;

public class AuditBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EnqueuedItem_GetsPersisted()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AuditContext>(o => o.UseInMemoryDatabase(databaseName));
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var queue = new ChannelAuditQueue(
            Options.Create(new AuditQueueOptions { Capacity = 10 }),
            NullLogger<ChannelAuditQueue>.Instance);
        var service = new AuditBackgroundService(queue, scopeFactory, NullLogger<AuditBackgroundService>.Instance);

        queue.Enqueue(new AuditEntry(AuditedEntityType.Claim, "claim-1", "POST", DateTimeOffset.UtcNow));

        await service.StartAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AuditContext>().UseInMemoryDatabase(databaseName).Options;
        using var context = new AuditContext(options);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!await context.ClaimAudits.AnyAsync(TestContext.Current.CancellationToken) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        await service.StopAsync(TestContext.Current.CancellationToken);

        var audit = await context.ClaimAudits.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("claim-1", audit.ClaimId);
        Assert.Equal("POST", audit.HttpRequestType);
    }
}
