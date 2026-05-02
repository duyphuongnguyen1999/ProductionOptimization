using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Abstractions.Events;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
