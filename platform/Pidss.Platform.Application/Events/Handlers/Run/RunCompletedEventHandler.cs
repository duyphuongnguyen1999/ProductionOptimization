using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;


namespace Pidss.Platform.Application.Events.Handlers.Run;

internal class RunCompletedEventHandler(
    ILogger<RunCompletedEventHandler> logger)
    : IDomainEventHandler<RunCompletedEvent>
{
    private readonly ILogger<RunCompletedEventHandler> _logger = logger;

    public Task HandleAsync(RunCompletedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Run with id {RunId} has completed",
            @event.RunId);

        return Task.CompletedTask;
    }
}