using BreastCancer.Enum;

namespace BreastCancer.Community.Workers.Fanout;

public sealed record FanoutJob(
    int PostId,
    string AuthorId,
    PostVisibility Visibility,
    DateTimeOffset Timestamp);
