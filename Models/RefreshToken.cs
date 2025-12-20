using Microsoft.EntityFrameworkCore.Storage;

namespace BreastCancer.Models
{
    public class RefreshToken
    {
        public int Id { set; get; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; }

        public bool IsActice => !IsRevoked && ExpiresAt < DateTime.UtcNow;
        
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

    }
}
