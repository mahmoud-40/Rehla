namespace BreastCancer.Community.DTO.response;

public sealed class FeedResponseDto
{
    public IReadOnlyList<PostDTO> Posts { get; init; } = Array.Empty<PostDTO>();
    public int? NextCursor { get; init; }
}
