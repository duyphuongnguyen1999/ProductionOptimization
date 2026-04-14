namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunCreatedEvent(Guid runId) : DomainEvent
{
    public Guid RunId { get; } = runId;
}