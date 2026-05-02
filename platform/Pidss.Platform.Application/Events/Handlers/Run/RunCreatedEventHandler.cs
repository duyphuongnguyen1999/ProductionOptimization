using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Run;

public class RunCreatedEventHandler(ILogger<RunCreatedEventHandler> logger) : IDomainEventHandler<RunCreatedEvent>
{
    private readonly ILogger<RunCreatedEventHandler> _logger = logger;

    public Task HandleAsync(RunCreatedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("Run {RunId} created", @event.RunId);
        return Task.CompletedTask;
    }
}