using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BreastCancer.Community.DTO.request
{
    public sealed class UpdatePostDTO
    {
        [Required]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 2000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(PostVisibility), ErrorMessage = "Invalid Post Visibility")]
        public PostVisibility Visibility { get; set; }

        [JsonPropertyName("mediaUrls")]
        public List<string>? MediaUrls { get; set; }
    }
}
