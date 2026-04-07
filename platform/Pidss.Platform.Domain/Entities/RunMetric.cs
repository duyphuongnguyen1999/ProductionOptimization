namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// A single KPI value extracted from analytics output and stored
/// in the database for fast querying and scenario comparison.
///
/// Domain execution data lives in JSON artifacts.
/// RunMetric is an indexed, queryable summary only.
///
/// Invariants:
///   - MetricKey is normalised to lowercase and trimmed
///   - MetricKey must not be empty
/// </summary>
public sealed class RunMetric
{
    public Guid Id { get; private init; }
    public Guid RunId { get; private init; }
    public string MetricKey { get; private init; } = string.Empty;
    public double MetricValue { get; private init; }
    public string? Unit { get; private init; }
    public DateTime RecordedAt { get; private init; }

    internal static RunMetric Create(
        Guid runId,
        string key,
        double value,
        string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Metric key must not be empty.", nameof(key));
        }

        return new RunMetric
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            MetricKey = key.Trim().ToLowerInvariant(),
            MetricValue = value,
            Unit = unit,
            RecordedAt = DateTime.UtcNow
        };
    }
}
