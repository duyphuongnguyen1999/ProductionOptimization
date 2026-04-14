using System.Text.Json.Nodes;

using Pidss.Platform.Domain.Abstractions;

namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// ComparisonRecord Aggregate Root.
///
/// Stores the result of an A/B comparison between two completed runs.
/// Comparison is a first-class REST resource — created once, stored, and retrievable.
///
/// Invariants:
///   - BaselineRunId and CandidateRunId must be different
///   - Result is immutable once set — comparison is never re-computed
///   - Comparison reads only from stored artifacts, never re-invokes engines
/// </summary>
public sealed class ComparisonRecord : AggregateRoot<Guid>
{
    public Guid BaselineRunId { get; private init; }
    public Guid CandidateRunId { get; private init; }
    public string ResultJson { get; private init; } = string.Empty;
    public DateTime CreatedAt { get; private init; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static ComparisonRecord Create(
        Guid baselineRunId,
        Guid candidateRunId,
        JsonObject result)
    {
        if (baselineRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "Baseline run ID must not be empty.", nameof(baselineRunId));
        }

        if (candidateRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "Candidate run ID must not be empty.", nameof(candidateRunId));
        }

        if (baselineRunId == candidateRunId)
        {
            throw new ArgumentException(
                "Baseline and candidate run IDs must be different.");
        }

        return new ComparisonRecord
        {
            Id = Guid.NewGuid(),
            BaselineRunId = baselineRunId,
            CandidateRunId = candidateRunId,
            ResultJson = result.ToJsonString(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Domain queries ─────────────────────────────────────────────────────

    public JsonObject GetResult() =>
        JsonNode.Parse(ResultJson)!.AsObject();
}
