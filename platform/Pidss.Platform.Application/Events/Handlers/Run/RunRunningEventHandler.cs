using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Run;

public class RunRunningEventHandler(
    ILogger<RunRunningEventHandler> logger)
    : IDomainEventHandler<RunRunningEvent>
{
    private readonly ILogger<RunRunningEventHandler> _logger = logger;

    public Task HandleAsync(RunRunningEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Run {RunId} started execution at {Time}",
            @event.RunId,
            DateTime.UtcNow);

        return Task.CompletedTask;
    }
}