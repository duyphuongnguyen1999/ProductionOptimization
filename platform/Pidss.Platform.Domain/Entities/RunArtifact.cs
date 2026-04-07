using Pidss.Platform.Domain.ValueObjects;

namespace Pidss.Platform.Domain.Entities;

/// <summary>
/// Metadata index record for one artifact file produced by a run.
/// The actual file lives on disk at <see cref="Path"/>.FullPath.
/// This entity is the queryable database reference.
///
/// Append-only: once created, an artifact record is never updated or deleted (ADR-0001).
/// </summary>
public sealed class RunArtifact
{
    public Guid Id { get; private init; }
    public Guid RunId { get; private init; }
    public string ArtifactType { get; private init; } = string.Empty;
    public ArtifactPath Path { get; private init; } = null!;
    public long SizeBytes { get; private init; }
    public string Sha256 { get; private init; } = string.Empty;
    public DateTime WrittenAt { get; private init; }

    /// <summary>Convenience accessor — delegates to <see cref="Path"/>.Filename.</summary>
    public string Filename => Path.Filename;

    /// <summary>Convenience accessor — delegates to <see cref="Path"/>.FullPath.</summary>
    public string FilePath => Path.FullPath;

    internal static RunArtifact Create(
        Guid runId,
        string artifactType,
        string runDirectory,
        string filename,
        long sizeBytes,
        string sha256) =>
        new()
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            ArtifactType = artifactType,
            Path = ArtifactPath.Create(runDirectory, filename),
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            WrittenAt = DateTime.UtcNow
        };
}
