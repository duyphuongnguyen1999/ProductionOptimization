using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobQueuedEventHandler(ILogger<JobQueuedEventHandler> logger)
    : IDomainEventHandler<JobQueuedEvent>
{
    private readonly ILogger<JobQueuedEventHandler> _logger = logger;
    public Task HandleAsync(JobQueuedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} has been queued",
            @event.JobId,
            @event.RunId);
        return Task.CompletedTask;
    }
}