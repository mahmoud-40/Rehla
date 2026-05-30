namespace BreastCancer.Community.DTO.response;

public sealed class FeedResponseDto
{
    public IReadOnlyList<int> PostIds { get; init; } = Array.Empty<int>();
    public int? NextCursor { get; init; }
}
