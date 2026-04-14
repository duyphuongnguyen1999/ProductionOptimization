namespace Pidss.Platform.Domain.DomainEvents;

public sealed class JobCompletedEvent(Guid jobId, Guid runId, int exitCode) : DomainEvent
{
    public Guid JobId { get; } = jobId;
    public Guid RunId { get; } = runId;
    public int ExitCode { get; } = exitCode;
}