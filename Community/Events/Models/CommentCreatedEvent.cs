namespace BreastCancer.Community.Events.Models;
public sealed record CommentCreatedEvent(
    int CommentId,
    int PostId,
    string AuthorId,
    string PostAuthorId,
    string Content,
    DateTime CreatedAt
) : DomainEvent;