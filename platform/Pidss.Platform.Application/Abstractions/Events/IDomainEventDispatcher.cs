using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
