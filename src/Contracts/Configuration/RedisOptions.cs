using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Configuration settings for the Redis connection.
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// The Redis connection string (e.g., "localhost:6379").
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
    }
}