using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobCompletedEventHandler(
    ILogger<JobCompletedEventHandler> logger)
    : IDomainEventHandler<JobCompletedEvent>
{
    private readonly ILogger<JobCompletedEventHandler> _logger = logger;
    public Task HandleAsync(JobCompletedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} has completed",
            @event.JobId,
            @event.RunId);
        return Task.CompletedTask;
    }
}