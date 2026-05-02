using Microsoft.Extensions.Logging;

using Pidss.Platform.Application.Abstractions.Events;
using Pidss.Platform.Domain.DomainEvents;

namespace Pidss.Platform.Application.Events.Handlers.Job;

public class JobRunningEventHandler(
    ILogger<JobRunningEventHandler> logger)
    : IDomainEventHandler<JobRunningEvent>
{
    private readonly ILogger<JobRunningEventHandler> _logger = logger;
    public Task HandleAsync(JobRunningEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Job with id {JobId}, belong to run with id {RunId} is running. Engine Version: {EngineVersion}",
            @event.JobId,
            @event.RunId,
            @event.EngineVersion);
        return Task.CompletedTask;
    }
}