namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobStartedEvent(Guid jobId, Guid runId, string engineVersion) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
    public string EngineVersion { get; } = engineVersion;
}