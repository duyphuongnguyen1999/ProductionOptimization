using Pidss.Platform.Domain.Abstractions;
using Pidss.Platform.Domain.Enums;

namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// Job Entity — internal to the Run aggregate.
///
/// Represents one engine invocation within a run.
/// Jobs are accessed only through <see cref="Run.Jobs"/> —
/// they are never referenced directly from outside the aggregate.
///
/// Invariants:
///   - Simulation (JobType = 1) always precedes Analytics (JobType = 2)
///   - Once in a terminal state (Completed, Failed), no further transitions are allowed
///   - EngineVersion is set when the engine process starts
/// </summary>
public sealed class Job : Entity<Guid>
{
    public Guid RunId { get; private init; }
    public JobType JobType { get; private init; }
    public JobStatus Status { get; private set; }
    public string? EngineVersion { get; private set; }
    public int? ExitCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DateTime CreatedAt { get; private init; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    internal static Job Create(Guid runId, JobType jobType) =>
        new()
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            JobType = jobType,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    // ── Status transitions ─────────────────────────────────────────────────

    internal void MarkQueued()
    {
        EnsureNotTerminal(JobStatus.Queued);
        Status = JobStatus.Queued;
    }

    internal void MarkRunning(string engineVersion)
    {
        EnsureNotTerminal(JobStatus.Running);
        Status = JobStatus.Running;
        EngineVersion = engineVersion;
        StartedAt = DateTime.UtcNow;
    }

    internal void MarkCompleted(int exitCode)
    {
        EnsureNotTerminal(JobStatus.Completed);
        Status = JobStatus.Completed;
        ExitCode = exitCode;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkFailed(int? exitCode, string errorMessage)
    {
        if (Status == JobStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Job '{Id}' ({JobType}) has already completed successfully.");
        }

        Status = JobStatus.Failed;
        ExitCode = exitCode;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkCancelled()
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException(
                $"Job '{Id}' ({JobType}) is already in terminal state '{Status}'.");
        }

        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    // ── Domain queries ─────────────────────────────────────────────────────

    public bool IsTerminal =>
        Status is JobStatus.Completed or JobStatus.Failed;

    // ── Invariants ─────────────────────────────────────────────────────────

    private void EnsureNotTerminal(JobStatus target)
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException(
                $"Job '{Id}' ({JobType}) is in terminal state '{Status}' " +
                $"and cannot transition to '{target}'.");
        }
    }
}
