using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Configuration settings for Kafka.
    /// </summary>
    public class KafkaOptions
    {
        /// <summary>
        /// The Kafka brokers address.
        /// </summary>
        [Required]
        public List<string> Brokers { get; set; } = new List<string>();
    }
}
