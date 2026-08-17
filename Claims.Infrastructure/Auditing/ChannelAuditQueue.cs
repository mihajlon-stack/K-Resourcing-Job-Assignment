using System.Threading.Channels;
using Claims.Application.Auditing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Bounded, non-blocking producer/consumer channel backing <see cref="IAuditQueue"/>.
/// Enqueueing never blocks the calling request; if the channel is saturated the entry
/// is dropped and logged with enough context to reconstruct it (decision #4).
/// </summary>
public class ChannelAuditQueue : IAuditQueue
{
    private readonly Channel<AuditEntry> _channel;
    private readonly ILogger<ChannelAuditQueue> _logger;

    public ChannelAuditQueue(IOptions<AuditQueueOptions> options, ILogger<ChannelAuditQueue> logger)
    {
        _logger = logger;
        // FullMode.Wait is required, not just the default: under DropWrite, TryWrite always
        // returns true (even for the entry it just silently discarded), which would make the
        // saturation check below never fire. Wait is safe here because we only ever call
        // TryWrite, never WriteAsync — so nothing actually blocks.
        _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(options.Value.Capacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public void Enqueue(AuditEntry entry)
    {
        if (!_channel.Writer.TryWrite(entry))
        {
            _logger.LogError(
                "Audit queue saturated; dropped entry for {EntityType} {EntityId} ({OperationType}) at {Timestamp}.",
                entry.EntityType, entry.EntityId, entry.HttpRequestType, entry.Timestamp);
        }
    }
}
