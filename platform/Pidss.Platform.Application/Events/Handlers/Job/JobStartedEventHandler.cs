using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobStartedEventHandler(
    ILogger<JobStartedEventHandler> logger)
    : IDomainEventHandler<JobStartedEvent>
{
    private readonly ILogger<JobStartedEventHandler> _logger = logger;
    public Task HandleAsync(JobStartedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} has started. Engine Version: {EngineVersion}",
            @event.JobId,
            @event.RunId,
            @event.EngineVersion);
        return Task.CompletedTask;
    }
}
