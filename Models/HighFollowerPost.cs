using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models;

public class HighFollowerPost
{
    public int Id { get; set; }
    public int PostId { get; set; }
    [MaxLength(450)]
    public string AuthorId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
