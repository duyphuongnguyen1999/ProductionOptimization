using System.Text.Json.Nodes;

using Pidss.Platform.Domain.ValueObjects;

namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// A named, reusable scenario configuration saved by users as
/// the basis for run submissions.
///
/// Unlike Run, ScenarioTemplate is fully mutable — supports PUT, PATCH, DELETE.
/// It is a first-class REST resource with full CRUD.
///
/// Invariants:
///   - Name must be non-empty and at most 200 characters
///   - SchemaVersion must be a valid MAJOR.MINOR string
///   - Tags are normalised: lowercase, deduplicated, sorted, comma-joined
/// </summary>
public sealed class ScenarioTemplate
{
    public Guid Id { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public SchemaVersion SchemaVersion { get; private set; } = null!;
    public string? Tags { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static ScenarioTemplate Create(
        string name,
        string? description,
        string schemaVersion,
        string? tags,
        JsonObject payload)
    {
        ValidateName(name);

        DateTime now = DateTime.UtcNow;
        return new ScenarioTemplate
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            SchemaVersion = SchemaVersion.Parse(schemaVersion),
            Tags = NormaliseTags(tags),
            PayloadJson = payload.ToJsonString(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // ── Full update (PUT) ──────────────────────────────────────────────────

    public void Update(
        string name,
        string? description,
        string schemaVersion,
        string? tags,
        JsonObject payload)
    {
        ValidateName(name);
        Name = name.Trim();
        Description = description?.Trim();
        SchemaVersion = SchemaVersion.Parse(schemaVersion);
        Tags = NormaliseTags(tags);
        PayloadJson = payload.ToJsonString();
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Partial update (PATCH) ─────────────────────────────────────────────

    public void Patch(
        string? name = null,
        string? description = null,
        string? tags = null)
    {
        if (name is not null)
        {
            ValidateName(name);
            Name = name.Trim();
        }

        if (description is not null)
        {
            Description = description.Trim();
        }

        if (tags is not null)
        {
            Tags = NormaliseTags(tags);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    // ── Domain queries ─────────────────────────────────────────────────────

    public JsonObject GetPayload() =>
        JsonNode.Parse(PayloadJson)!.AsObject();

    public IReadOnlyList<string> GetTagList() =>
        string.IsNullOrWhiteSpace(Tags)
            ? []
            : Tags.Split(',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

    // ── Invariants ─────────────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "ScenarioTemplate name must not be empty.", nameof(name));
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "ScenarioTemplate name must not exceed 200 characters.", nameof(name));
        }
    }

    private static string? NormaliseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        IOrderedEnumerable<string> normalised = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .OrderBy(t => t);

        return string.Join(",", normalised);
    }
}
