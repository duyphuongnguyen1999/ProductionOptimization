namespace Pidss.Platform.Domain.DomainEvents;

public sealed class RunCancelledEvent(Guid runId, string? reason = null) : DomainEvent
{
    public Guid RunId { get; } = runId;
    public string? Reason { get; } = reason;
}