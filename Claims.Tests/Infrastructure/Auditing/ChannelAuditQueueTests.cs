using Claims.Application.Auditing;
using Claims.Infrastructure.Auditing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Claims.Tests.Infrastructure.Auditing;

public class ChannelAuditQueueTests
{
    private static ChannelAuditQueue CreateQueue(int capacity, RecordingLogger? logger = null) =>
        new(Options.Create(new AuditQueueOptions { Capacity = capacity }), logger ?? new RecordingLogger());

    private static AuditEntry SampleEntry(string id = "entity-1") =>
        new(AuditedEntityType.Claim, id, "POST", DateTimeOffset.UtcNow);

    [Fact]
    public void Enqueue_WithinCapacity_IsReadable()
    {
        var queue = CreateQueue(capacity: 2);

        queue.Enqueue(SampleEntry());

        Assert.True(queue.Reader.TryRead(out var entry));
        Assert.Equal("entity-1", entry.EntityId);
    }

    [Fact]
    public void Enqueue_BeyondCapacity_DropsEntryAndLogsError()
    {
        var logger = new RecordingLogger();
        var queue = CreateQueue(capacity: 1, logger);

        queue.Enqueue(SampleEntry("kept"));
        queue.Enqueue(SampleEntry("dropped"));

        Assert.True(queue.Reader.TryRead(out var kept));
        Assert.Equal("kept", kept.EntityId);
        Assert.False(queue.Reader.TryRead(out _));

        Assert.Equal(1, logger.ErrorCount);
    }

    /// <summary>
    /// Minimal <see cref="ILogger{TCategoryName}"/> test double. NSubstitute cannot cleanly
    /// verify the generic <c>Log&lt;TState&gt;</c> call because the framework's real TState
    /// (an internal formatted-log-values type) never matches a substitute configured for a
    /// different TState, so a small hand-written recorder is used instead.
    /// </summary>
    private class RecordingLogger : ILogger<ChannelAuditQueue>
    {
        public int ErrorCount { get; private set; }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                ErrorCount++;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
