namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobFailedEvent(Guid jobId, Guid runId, string errorMessage) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
    public string ErrorMessage { get; } = errorMessage;
}