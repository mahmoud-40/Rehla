namespace BreastCancer.Community.DTO.response;

public class UserPostsResponseDto
{
    public List<PostDTO> Posts { get; set; } = new();
    public string? NextCursor { get; set; }
    public int Total { get; set; }
}