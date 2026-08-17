namespace Claims.Application.Auditing;

public enum AuditedEntityType
{
    Claim,
    Cover
}

public record AuditEntry(AuditedEntityType EntityType, string EntityId, string HttpRequestType, DateTimeOffset Timestamp);
