namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunCompletedEvent(Guid runId) : DomainEvent
{
    public Guid RunId { get; } = runId;
}