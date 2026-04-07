using Pidss.Platform.Domain.Enums;
using Pidss.Platform.Domain.ValueObjects;

namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// Represents one immutable scenario evaluation run — the top-level execution unit.
/// Owns Job entities, RunArtifact index records, and RunMetric summaries.
///
/// External code interacts with jobs, artifacts, and metrics only through
/// this root — never by referencing the internal entities directly.
///
/// Invariants:
///   - Status transitions are forward-only
///   - Completed, Failed, and Cancelled are terminal states
///   - Domain execution data is NOT stored here — lives in JSON artifacts on disk
///   - Append-only: a Run is never deleted (ADR-0001)
/// </summary>
public sealed class Run
{
    public Guid Id { get; private init; }
    public RunStatus Status { get; private set; }
    public SchemaVersion? SchemaVersion { get; private init; }
    public string? CalibrationProfileId { get; private init; }
    public string ArtifactDirectory { get; private init; } = string.Empty;

    public DateTime CreatedAt { get; private init; }
    public DateTime? QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }
    public string? ErrorDetail { get; private set; }

    // Internal entities — accessible only through this aggregate root.
    private readonly List<Job> _jobs = [];
    private readonly List<RunArtifact> _artifacts = [];
    private readonly List<RunMetric> _metrics = [];

    public IReadOnlyList<Job> Jobs => _jobs.AsReadOnly();
    public IReadOnlyList<RunArtifact> Artifacts => _artifacts.AsReadOnly();
    public IReadOnlyList<RunMetric> Metrics => _metrics.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static Run Create(
        Guid runId,
        string? schemaVersion,
        string? calibrationProfileId,
        string artifactDirectory)
    {
        if (string.IsNullOrWhiteSpace(artifactDirectory))
        {
            throw new ArgumentException(
                "Artifact directory must not be empty.", nameof(artifactDirectory));
        }

        SchemaVersion? parsedVersion = null;
        if (!string.IsNullOrWhiteSpace(schemaVersion))
        {
            SchemaVersion.TryParse(schemaVersion, out parsedVersion);
        }

        var run = new Run
        {
            Id = runId,
            Status = RunStatus.Created,
            SchemaVersion = parsedVersion,
            CalibrationProfileId = calibrationProfileId,
            ArtifactDirectory = artifactDirectory,
            CreatedAt = DateTime.UtcNow
        };

        // Initialise jobs in execution order.
        run._jobs.Add(Job.Create(runId, JobType.Simulation));
        run._jobs.Add(Job.Create(runId, JobType.Analytics));

        return run;
    }

    // ── Job access (through aggregate root) ───────────────────────────────

    public Job GetJob(JobType jobType) =>
        _jobs.FirstOrDefault(j => j.JobType == jobType)
        ?? throw new InvalidOperationException(
            $"Job of type '{jobType}' not found in run '{Id}'.");

    public void MarkJobQueued(JobType jobType) =>
        GetJob(jobType).MarkQueued();

    public void MarkJobRunning(JobType jobType, string engineVersion) =>
        GetJob(jobType).MarkRunning(engineVersion);

    public void MarkJobCompleted(JobType jobType, int exitCode) =>
        GetJob(jobType).MarkCompleted(exitCode);

    public void MarkJobFailed(JobType jobType, int? exitCode, string errorMessage) =>
        GetJob(jobType).MarkFailed(exitCode, errorMessage);

    // ── Artifact management (through aggregate root) ───────────────────────

    public RunArtifact AddArtifact(
        string artifactType,
        string filename,
        long sizeBytes,
        string sha256)
    {
        var artifact = RunArtifact.Create(Id, artifactType, ArtifactDirectory,
            filename, sizeBytes, sha256);
        _artifacts.Add(artifact);
        return artifact;
    }

    // ── Metric management (through aggregate root) ─────────────────────────

    public RunMetric AddMetric(string key, double value, string? unit = null)
    {
        var metric = RunMetric.Create(Id, key, value, unit);
        _metrics.Add(metric);
        return metric;
    }

    // ── Status transitions ─────────────────────────────────────────────────

    public void TransitionTo(
        RunStatus next,
        string? errorMessage = null,
        string? errorDetail = null)
    {
        ValidateTransition(Status, next);
        Status = next;

        switch (next)
        {
            case RunStatus.Queued:
                QueuedAt = DateTime.UtcNow;
                break;
            case RunStatus.Running:
                StartedAt = DateTime.UtcNow;
                break;
            case RunStatus.Completed:
            case RunStatus.Failed:
            case RunStatus.Cancelled:
                CompletedAt = DateTime.UtcNow;
                ErrorMessage = errorMessage;
                ErrorDetail = errorDetail;
                break;
        }
    }

    // ── Domain queries ─────────────────────────────────────────────────────

    public bool IsTerminal =>
        Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Cancelled;

    public bool IsCancellable =>
        Status is RunStatus.Created or RunStatus.Validating or RunStatus.Queued;

    // ── Invariants ─────────────────────────────────────────────────────────

    private static void ValidateTransition(RunStatus current, RunStatus next)
    {
        if (current is RunStatus.Completed or RunStatus.Failed or RunStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Run is in terminal state '{current}' and cannot transition to '{next}'.");
        }

        // Failed and Cancelled are always reachable from any non-terminal state.
        if (next is RunStatus.Failed or RunStatus.Cancelled)
        {
            return;
        }

        if ((int)next <= (int)current)
        {
            throw new InvalidOperationException(
                $"Invalid run status transition from '{current}' to '{next}'. " +
                "Transitions must move forward in the lifecycle.");
        }
    }
}
