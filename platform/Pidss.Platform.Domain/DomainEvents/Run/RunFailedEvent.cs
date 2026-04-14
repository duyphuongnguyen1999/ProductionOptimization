namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunFailedEvent(Guid runId, string errorMessage) : DomainEvent
{
    public Guid RunId { get; } = runId;
    public string ErrorMessage { get; } = errorMessage;
}