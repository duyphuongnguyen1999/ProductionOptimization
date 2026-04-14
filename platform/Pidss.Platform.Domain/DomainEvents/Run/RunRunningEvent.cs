namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunRunningEvent(Guid runId) : DomainEvent
{
    public Guid RunId { get; } = runId;
}