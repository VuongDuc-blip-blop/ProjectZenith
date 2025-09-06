namespace FileScan.Functions.Services.VirusTotal
{
    public interface IVirusScanService
    {
        public enum ScanResultStatus { Safe, Malicious, ScanError, Timeout }

        public record ScanResult(ScanResultStatus Status, string? Details);
        Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken);
    }
}
