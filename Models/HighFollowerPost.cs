namespace BreastCancer.Models;

public class HighFollowerPost
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string AuthorId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
