namespace Pidss.Platform.Domain.Abstractions;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides identity-based equality.
///
/// Two entities are equal if and only if:
///   - They are of the same concrete type
///   - They share the same Id value
///
/// Subclasses should never override Equals/GetHashCode.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        if (id is Guid g && g == Guid.Empty)
        {
            throw new ArgumentException("Entity Id must not be an empty Guid.", nameof(id));
        }

        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return obj is Entity<TId> other && Id.Equals(other.Id);
    }

    public override int GetHashCode() =>
        HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? (right is null);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !(left == right);
}
