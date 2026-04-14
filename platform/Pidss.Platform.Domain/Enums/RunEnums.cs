namespace Pidss.Platform.Domain.Enums;

/// <summary>
/// Lifecycle states for a PIDSS run.
/// Transitions are forward-only; Completed, Failed and Cancelled are terminal.
///
/// Created → Validating → Queued → Running → Completed
///                                          → Failed
///        → Failed (validation failure)
///        → Cancelled (user request, only from pre-Running states)
/// </summary>
public enum RunStatus
{
    Created,
    Validating,
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Lifecycle states for an engine job within a run.
/// Transitions are forward-only; Completed and Failed are terminal.
///
/// Pending → Queued → Running → Completed
///                            → Failed
/// </summary>
public enum JobStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// The two engine jobs executed sequentially in every run.
/// Simulation must complete successfully before Analytics may start.
/// </summary>
public enum JobType
{
    Simulation = 1,
    Analytics = 2,
    Optimization = 3
}
