using MediatR;

namespace BreastCancer.Community.Domain;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
