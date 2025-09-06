namespace ProjectZenith.Contracts.Configuration
{
    public class VirusTotalOptions
    {
        // The name of the configuration section in user secrets
        public const string SectionName = "VirusTotal";

        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://www.virustotal.com/api/v3/";
    }
}
