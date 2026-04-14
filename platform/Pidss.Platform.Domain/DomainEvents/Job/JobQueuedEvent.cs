namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobQueuedEvent(Guid jobId, Guid runId) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
}