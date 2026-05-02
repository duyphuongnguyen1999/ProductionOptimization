using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Infrastructure.Events;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (IDomainEvent @event in events)
        {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());
            IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerType);

            foreach (object? handler in handlers)
            {
                if (handler is not IDomainEventHandler<IDomainEvent> domainEventHandler)
                {
                    continue;
                }

                MethodInfo? method = handlerType.GetMethod(nameof(IDomainEventHandler<>.HandleAsync));

                if (method != null)
                {
                    await domainEventHandler.HandleAsync(@event, ct);
                }
            }
        }
    }
}
