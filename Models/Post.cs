using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string AuthorId { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public PostType Type { get; set; } = PostType.Story;
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public List<string> MediaUrls { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsEdited { get; set; }

        public bool IsDeleted { get; set; } = false;

        public virtual ApplicationUser Author { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}
