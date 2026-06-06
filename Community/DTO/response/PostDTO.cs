using BreastCancer.Enum;
using System.Text.Json.Serialization;

namespace BreastCancer.Community.DTO.response
{
    public class PostDTO
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public string Content { get; set; }
        public PostType PostType { get; set; }
        public PostVisibility PostVisibility { get; set; }
        [JsonPropertyName("mediaUrls")]
        public IReadOnlyCollection<string>? MediaUrls { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsEdited { get; set; }

        public Dictionary<string, long> ReactionCounts { get; set; } = new();
    }
}
