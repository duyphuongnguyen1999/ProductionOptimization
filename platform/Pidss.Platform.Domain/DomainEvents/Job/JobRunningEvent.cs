namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobRunningEvent(Guid jobId, Guid runId, string engineVersion) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
    public string EngineVersion { get; } = engineVersion;
}