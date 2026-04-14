namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunQueuedEvent(Guid runId) : DomainEvent
{
    public Guid RunId { get; } = runId;
}