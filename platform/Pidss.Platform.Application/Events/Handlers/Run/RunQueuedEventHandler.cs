using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.EventHandlers.Run;

public class RunQueuedEventHandler(ILogger<RunQueuedEventHandler> logger) : IDomainEventHandler<RunQueuedEvent>
{
    private readonly ILogger<RunQueuedEventHandler> _logger = logger;

    public Task HandleAsync(RunQueuedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("Run {RunId} queued", @event.RunId);
        return Task.CompletedTask;
    }
}