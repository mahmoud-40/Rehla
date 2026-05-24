using BreastCancer.Enum;

namespace BreastCancer.Community.Events;

public sealed record FanoutJob(
    int PostId,
    string AuthorId,
    PostVisibility Visibility,
    DateTimeOffset Timestamp);
