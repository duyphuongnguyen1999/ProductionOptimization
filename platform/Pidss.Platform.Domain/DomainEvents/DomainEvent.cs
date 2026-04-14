namespace Pidss.Platform.Domain.DomainEvents;

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}