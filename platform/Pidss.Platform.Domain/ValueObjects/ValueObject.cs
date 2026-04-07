namespace Pidss.Platform.Domain.ValueObjects;

/// <summary>
/// Abstract base class for all Value Objects.
///
/// Value Objects are immutable — all properties must have private or init-only setters.
/// Two Value Objects are equal if all their equality components are equal.
///
/// Subclasses must implement <see cref="GetEqualityComponents"/> to define
/// what constitutes equality for that value object.
/// </summary>
public abstract class ValueObject
{
    public abstract IEnumerable<object?> GetEqualityComponents();

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

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left?.Equals(right) ?? (right is null);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
