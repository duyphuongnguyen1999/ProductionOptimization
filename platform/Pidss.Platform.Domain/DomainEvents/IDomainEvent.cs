namespace Pidss.Platform.Domain.DomainEvents;

/// <summary>
/// Marker interface for all domain events.
///
/// Domain events represent something that happened in the domain
/// that other parts of the system may need to react to.
///
/// In PIDSS v1 these are collected on the AggregateRoot and cleared
/// after persistence. Dispatching is wired in Phase 9+ (ML pipeline).
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredAt { get; }
}
