namespace Pidss.Platform.Domain.ValueObjects;

/// <summary>
/// Represents a versioned public scenario schema identifier (e.g. "1.0").
///
/// Invariants:
///   - Format must be MAJOR.MINOR
///   - Both MAJOR and MINOR must be non-negative integers
///   - Empty or whitespace values are rejected at construction time
///
/// Produced by the Adapter layer. Stored on ScenarioTemplate and Run
/// to record which schema contract was used.
/// </summary>
public sealed class SchemaVersion : ValueObject
{
    public int Major { get; }
    public int Minor { get; }

    private SchemaVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    /// <summary>
    /// Parses a schema version string.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the format is invalid.</exception>
    public static SchemaVersion Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Schema version must not be empty.", nameof(value));
        }

        string[] parts = value.Trim().Split('.');

        if (parts.Length != 2
            || !int.TryParse(parts[0], out int major)
            || !int.TryParse(parts[1], out int minor)
            || major < 0
            || minor < 0)
        {
            throw new ArgumentException(
                $"Invalid schema version '{value}'. Expected format: MAJOR.MINOR (e.g. '1.0').",
                nameof(value));
        }

        return new SchemaVersion(major, minor);
    }

    /// <summary>
    /// Tries to parse a schema version string without throwing.
    /// </summary>
    public static bool TryParse(string? value, out SchemaVersion? result)
    {
        try
        {
            result = Parse(value ?? string.Empty);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public override string ToString() => $"{Major}.{Minor}";

    public override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
    }

    public static implicit operator string(SchemaVersion v) => v.ToString();
}
