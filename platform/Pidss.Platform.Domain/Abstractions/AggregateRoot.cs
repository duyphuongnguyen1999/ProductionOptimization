using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Domain.Abstractions;

/// <summary>
/// Abstract base class for all Aggregate Roots.
/// Provides identity-based equality on top of <see cref="Entity{TId}"/>.
///
/// Manages a list of pending domain events that are raised during
/// aggregate state changes and dispatched by the application layer
/// after the aggregate is persisted.
/// 
/// Subclasses call <see cref="RaiseEvent"/> inside their domain methods.
/// The application layer calls <see cref="ClearEvents"/> after dispatch.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
    }

    public void ClearEvents()
    {
        _events.Clear();
    }

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }
}