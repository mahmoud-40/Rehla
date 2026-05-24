using BreastCancer.Community.Domain;
using BreastCancer.Enum;

namespace BreastCancer.Community.Events;

public sealed record PostCreatedEvent(int PostId, string AuthorId, PostVisibility Visibility) : DomainEvent;
