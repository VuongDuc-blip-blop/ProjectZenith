using System.Text.Json.Serialization;

namespace FileScan.Functions.Services.VirusTotal.DTO
{
    // Result of asking for an upload URL
    public class FileUploadUrlResponse
    {
        [JsonPropertyName("data")]
        public string? UploadUrl { get; set; }
    }

    // Result of getting an analysis report
    public class AnalysisReportResponse
    {
        [JsonPropertyName("data")]
        public AnalysisData? Data { get; set; }
    }

    public class AnalysisData
    {
        [JsonPropertyName("attributes")]
        public AnalysisAttributes? Attributes { get; set; }
    }

    public class AnalysisAttributes
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } // e.g., "queued", "inprogress", "completed"

        [JsonPropertyName("stats")]
        public AnalysisStats? Stats { get; set; }
    }

    public class AnalysisStats
    {
        [JsonPropertyName("malicious")]
        public int Malicious { get; set; }

        [JsonPropertyName("suspicious")]
        public int Suspicious { get; set; }

        [JsonPropertyName("undetected")]
        public int Undetected { get; set; }
    }

    // Represents the response from a successful file upload POST
    public class FileUploadResponse
    {
        [JsonPropertyName("data")]
        public UploadData? Data { get; set; }
    }

    public class UploadData
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // Should be "analysis"

        [JsonPropertyName("id")]
        public string? Id { get; set; } // This is the Analysis ID we need
    }
}
