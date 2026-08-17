using Claims.Application.Auditing;
using Claims.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Drains the audit channel and persists each entry via <see cref="AuditContext"/>.
/// The try/catch sits inside the per-item iteration, not around the outer loop: since
/// .NET 6, an exception escaping <see cref="ExecuteAsync"/> stops the entire host
/// (BackgroundServiceExceptionBehavior.StopHost), so a single bad write must never
/// escape the loop or it can take down the process, not just audit processing.
/// </summary>
public class AuditBackgroundService : BackgroundService
{
    private readonly ChannelAuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditBackgroundService> _logger;

    public AuditBackgroundService(
        ChannelAuditQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var entry in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await PersistAsync(entry, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to persist audit entry for {EntityType} {EntityId} ({OperationType}) at {Timestamp}.",
                    entry.EntityType, entry.EntityId, entry.HttpRequestType, entry.Timestamp);
            }
        }
    }

    private async Task PersistAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();

        if (entry.EntityType == AuditedEntityType.Claim)
        {
            auditContext.ClaimAudits.Add(new ClaimAudit
            {
                ClaimId = entry.EntityId,
                HttpRequestType = entry.HttpRequestType,
                Created = entry.Timestamp
            });
        }
        else
        {
            auditContext.CoverAudits.Add(new CoverAudit
            {
                CoverId = entry.EntityId,
                HttpRequestType = entry.HttpRequestType,
                Created = entry.Timestamp
            });
        }

        await auditContext.SaveChangesAsync(cancellationToken);
    }
}
