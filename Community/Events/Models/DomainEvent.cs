using MediatR;

namespace BreastCancer.Community.Events.Models;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
