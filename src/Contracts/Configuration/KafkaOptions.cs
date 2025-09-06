using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Configuration settings for Kafka.
    /// </summary>
    public class KafkaOptions
    {
        [Required(AllowEmptyStrings = false)]
        public string BootstrapServers { get; set; } = "localhost:9093";

        public const string SectionName = "Kafka";
    }
}
