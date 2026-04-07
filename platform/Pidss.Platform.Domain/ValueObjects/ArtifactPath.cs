namespace Pidss.Platform.Domain.ValueObjects;

/// <summary>
/// Represents the validated filesystem path to a PIDSS run artifact.
///
/// Encapsulates the convention: artifacts/{run_id}/{filename}
///
/// Invariants:
///   - Filename must not be empty
///   - Filename must not contain path traversal sequences (.., /, \)
///   - FullPath is always the combination of runDirectory and filename
///
/// Prevents path traversal attacks at domain level.
/// </summary>
public sealed class ArtifactPath : ValueObject
{
    public string FullPath { get; }
    public string Filename { get; }
    public string Directory { get; }

    private ArtifactPath(string fullPath, string filename, string directory)
    {
        FullPath = fullPath;
        Filename = filename;
        Directory = directory;
    }

    /// <summary>
    /// Creates a validated ArtifactPath from a run directory and filename.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if inputs are invalid.</exception>
    public static ArtifactPath Create(string runDirectory, string filename)
    {
        if (string.IsNullOrWhiteSpace(runDirectory))
        {
            throw new ArgumentException(
                "Run directory must not be empty.", nameof(runDirectory));
        }

        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException(
                "Artifact filename must not be empty.", nameof(filename));
        }

        if (filename.Contains("..") ||
            filename.Contains('/') ||
            filename.Contains('\\'))
        {
            throw new ArgumentException(
                $"Invalid artifact filename '{filename}'. Path traversal sequences are not allowed.",
                nameof(filename));
        }

        string fullPath = Path.Combine(runDirectory, filename);
        return new ArtifactPath(fullPath, filename, runDirectory);
    }

    public bool Exists() => File.Exists(FullPath);

    public override string ToString() => FullPath;

    public override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FullPath.ToLowerInvariant();
    }
}
