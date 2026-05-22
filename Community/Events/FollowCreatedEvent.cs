using BreastCancer.Community.Domain;

namespace BreastCancer.Community.Events;

public sealed record FollowCreatedEvent(string FollowerId, string FollowingId) : DomainEvent;
