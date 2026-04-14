namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunValidatingEvent(Guid runId) : DomainEvent
{
    public Guid RunId { get; } = runId;
}