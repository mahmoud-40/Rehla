using BreastCancer.Enum;

namespace BreastCancer.Community.Events.Models;

public sealed record PostCreatedEvent(int PostId, string AuthorId, PostVisibility Visibility) : DomainEvent;
