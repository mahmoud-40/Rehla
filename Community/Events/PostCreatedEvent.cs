using BreastCancer.Community.Domain;

namespace BreastCancer.Community.Events;

public sealed record PostCreatedEvent(int PostId, string AuthorId) : DomainEvent;
