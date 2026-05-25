namespace BreastCancer.Community.Events.Models;

public sealed record FollowCreatedEvent(string FollowerId, string FollowingId) : DomainEvent;
