using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Run;

public class RunFailedEventHandler(
    ILogger<RunFailedEventHandler> logger)
    : IDomainEventHandler<RunFailedEvent>
{
    private readonly ILogger<RunFailedEventHandler> _logger = logger;

    public Task HandleAsync(RunFailedEvent @event, CancellationToken ct = default)
    {
        _logger.LogError(
            "Run with id {RunId} has failed with error: {Error}",
            @event.RunId,
            @event.ErrorMessage);

        return Task.CompletedTask;
    }
}