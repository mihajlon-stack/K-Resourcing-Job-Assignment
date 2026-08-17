namespace Claims.Application.Auditing;

/// <summary>
/// Non-blocking producer side of the audit pipeline. Enqueueing never awaits I/O and
/// never throws; a saturated queue drops the entry rather than applying backpressure
/// to the caller (see decision #4 in the project README).
/// </summary>
public interface IAuditQueue
{
    void Enqueue(AuditEntry entry);
}
