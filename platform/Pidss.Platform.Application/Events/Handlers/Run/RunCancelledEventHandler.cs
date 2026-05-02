using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;


namespace Pidss.Platform.Application.Events.Handlers.Run;

internal class RunCancelledEventHandler(
    ILogger<RunCancelledEventHandler> logger)
    : IDomainEventHandler<RunCancelledEvent>
{
    private readonly ILogger<RunCancelledEventHandler> _logger = logger;
    public Task HandleAsync(RunCancelledEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Run with id {RunId} has been cancelled with reason: {Reason}",
            @event.RunId,
            @event.Reason);
        return Task.CompletedTask;
    }
}
