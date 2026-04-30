using Pidss.Platform.Domain.Abstractions;

namespace Pidss.Platform.Domain.ValueObjects;

/// <summary>
/// Represents the stage attribution weight map for an integrated WorkUnitModel.
///
/// An integrated WorkUnitModel covers multiple SOP stages. Stage weights define
/// what fraction of the unit's capacity is attributed to each covered stage —
/// enabling per-stage KPI reporting and bottleneck detection.
///
/// Invariants:
///   - Must contain at least one entry
///   - All stage IDs must be non-empty
///   - All weight values must be positive
///   - Weights must sum to exactly 1.0 (within tolerance of 1e-6)
///
/// Produced exclusively by ScenarioAdapterV1.
/// Engines consume pre-materialized weights — never compute attribution themselves.
/// </summary>
public sealed class StageWeight : ValueObject
{
    private const double SumTolerance = 1e-6;

    /// <summary>Immutable map of stage_id → weight fraction (0 &lt; w ≤ 1).</summary>
    public IReadOnlyDictionary<string, double> Weights { get; }

    private StageWeight(IReadOnlyDictionary<string, double> weights) =>
        Weights = weights;

    /// <summary>
    /// Creates a StageWeight map from explicit weights.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if any invariant is violated.</exception>
    public static StageWeight Create(IDictionary<string, double> weights)
    {
        if (weights is null || weights.Count == 0)
        {
            throw new ArgumentException(
                "Stage weights must contain at least one entry.", nameof(weights));
        }

        foreach ((string? stageId, double w) in weights)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                throw new ArgumentException(
                    "Stage ID in weight map must not be empty.", nameof(weights));
            }

            if (w <= 0)
            {
                throw new ArgumentException(
                    $"Weight for stage '{stageId}' must be positive (got {w}).",
                    nameof(weights));
            }
        }

        double sum = weights.Values.Sum();
        if (Math.Abs(sum - 1.0) > SumTolerance)
        {
            throw new ArgumentException(
                $"Stage weights must sum to 1.0 (got {sum:F8}, tolerance ±{SumTolerance}).",
                nameof(weights));
        }

        return new StageWeight(new Dictionary<string, double>(weights));
    }

    /// <summary>
    /// Creates a StageWeight map using equal-share distribution.
    /// The last stage absorbs any floating-point rounding remainder.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if stageIds is empty.</exception>
    public static StageWeight EqualShare(IReadOnlyList<string> stageIds)
    {
        if (stageIds is null || stageIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one stage ID is required.", nameof(stageIds));
        }

        double weight = Math.Round(1.0 / stageIds.Count, 10);
        var map = new Dictionary<string, double>();

        for (int i = 0; i < stageIds.Count; i++)
        {
            map[stageIds[i]] = i == stageIds.Count - 1
                ? Math.Round(1.0 - (weight * (stageIds.Count - 1)), 10)
                : weight;
        }

        return new StageWeight(map);
    }

    public double this[string stageId] => Weights[stageId];

    public bool ContainsStage(string stageId) => Weights.ContainsKey(stageId);

    public override string ToString() =>
        string.Join(", ", Weights.Select(kv => $"{kv.Key}:{kv.Value:F4}"));

    public override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (KeyValuePair<string, double> kv in Weights.OrderBy(x => x.Key))
        {
            yield return kv.Key;
            yield return kv.Value;
        }
    }
}
