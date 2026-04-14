namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobCancelledEvent(Guid jobId, Guid runId, string? reason = null) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
    public string? Reason { get; } = reason;
}