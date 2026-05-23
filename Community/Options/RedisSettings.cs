using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Community.Options;

public class RedisSettings
{
    public const string RedisSettingsKey = "Redis";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Redis ConnectionString is required and cannot be empty")]
    public string ConnectionString { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Redis DefaultTTLSeconds must be at least 1 second")]
    public int DefaultTTLSeconds { get; init; } = 3600; // 1 hour default
}
