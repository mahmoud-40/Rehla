namespace BreastCancer.Community.DTO.response;

public class CommentResponseDTO
{
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public string AuthorId { get; set; }
    public string AuthorName { get; set; }
    public string AuthorAvaterUrl { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; init; }
}