using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobFailedEventHandler(
    ILogger<JobFailedEventHandler> logger)
    : IDomainEventHandler<JobFailedEvent>
{
    private readonly ILogger<JobFailedEventHandler> _logger = logger;
    public Task HandleAsync(JobFailedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} has failed. Error: {ErrorMessage}",
            @event.JobId,
            @event.RunId,
            @event.ErrorMessage);
        return Task.CompletedTask;
    }
}