using System.Text.Json.Serialization;

namespace BreastCancer.DTO.response
{
    public class TokenResponseDTO
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresTime { get; set; }

        [JsonIgnore]
        public bool IsSuccess { get; set; }
        [JsonIgnore]
        public IEnumerable<string>? Errors { get; set; } 

    }
}

