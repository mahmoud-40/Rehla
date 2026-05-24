namespace BreastCancer.Community.Options;

public sealed class CommunityOptions
{
    public const string CommunityOptionsKey = "Community";

    public int FanoutPushThreshold { get; init; } = 500; // default
}
