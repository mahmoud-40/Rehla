using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public string AuthorId { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;
   
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public virtual Post Post { get; set; } = null!;
        public virtual ApplicationUser Author { get; set; } = null!;
    }
}
