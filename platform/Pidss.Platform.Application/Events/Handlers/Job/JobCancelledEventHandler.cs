using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobCancelledEventHandler(
    ILogger<JobCancelledEventHandler> logger)
    : IDomainEventHandler<JobCancelledEvent>
{
    private readonly ILogger<JobCancelledEventHandler> _logger = logger;
    public Task HandleAsync(JobCancelledEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} has been cancelled. Reason: {Reason}",
            @event.JobId,
            @event.RunId,
            @event.Reason);
        return Task.CompletedTask;
    }
}