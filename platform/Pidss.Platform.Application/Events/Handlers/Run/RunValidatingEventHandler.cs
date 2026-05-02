using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Run;

public class RunValidatingEventHandler(
    ILogger<RunValidatingEventHandler> logger)
    : IDomainEventHandler<RunValidatingEvent>
{
    private readonly ILogger<RunValidatingEventHandler> _logger = logger;

    public Task HandleAsync(RunValidatingEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Run with id {RunId} is validating",
            @event.RunId);

        return Task.CompletedTask;
    }
}
