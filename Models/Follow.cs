using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Follow
    {
        [Required]
        public string FollowerId { get; set; } = null!;

        [Required]
        public string FollowingId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser Follower { get; set; } = null!;
        public virtual ApplicationUser Following { get; set; } = null!;
    }
}
