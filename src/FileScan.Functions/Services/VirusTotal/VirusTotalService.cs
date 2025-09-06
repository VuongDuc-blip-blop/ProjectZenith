// In ProjectZenith.FileProcessor/Services/VirusTotalService.cs
using FileScan.Functions.Services.VirusTotal;
using FileScan.Functions.Services.VirusTotal.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using static FileScan.Functions.Services.VirusTotal.IVirusScanService;

public class VirusTotalService : IVirusScanService
{
    private readonly HttpClient _httpClient;
    private readonly VirusTotalOptions _options;
    private readonly ILogger<VirusTotalService> _logger;
    private const int POLLING_INTERVAL_MS = 20000; // 20 seconds
    private const int MAX_POLLING_ATTEMPTS = 15; // 15 attempts * 20s = 5 minutes timeout

    public VirusTotalService(HttpClient httpClient, IOptions<VirusTotalOptions> options, ILogger<VirusTotalService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-apikey", _options.ApiKey);
    }

    public async Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken)
    {
        try
        {

            string? uploadUrl;

            if (fileSize < 32 * 1024 * 1024) // < 32MB
            {
                // Use direct /files endpoint
                uploadUrl = $"{_options.BaseUrl.TrimEnd('/')}/files";
            }
            else
            {
                // Step 1: Get Upload URL
                _logger.LogInformation("Getting VirusTotal upload URL for '{fileName}'.", fileName);
                uploadUrl = await GetUploadUrlAsync(cancellationToken);
                if (string.IsNullOrEmpty(uploadUrl))
                {
                    return new ScanResult(ScanResultStatus.ScanError, "Failed to get an upload URL from VirusTotal.");
                }
            }

            // Step 2: Upload the file
            _logger.LogInformation("Uploading '{fileName}' to VirusTotal.", fileName);
            var analysisId = await UploadFileAsync(uploadUrl, fileStream, fileName, cancellationToken);
            if (string.IsNullOrEmpty(analysisId))
            {
                return new ScanResult(ScanResultStatus.ScanError, "Failed to upload file or get an analysis ID.");
            }

            // Step 3 & 4: Poll for results
            _logger.LogInformation("Polling for analysis results for ID '{analysisId}'.", analysisId);
            return await PollForCompletionAsync(analysisId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
               "An unexpected error occurred while processing file '{fileName}': {message} \n StackTrace: {stackTrace}",
               fileName, ex.Message, ex.StackTrace);
            return new ScanResult(ScanResultStatus.ScanError, ex.Message);
        }
    }

    private async Task<string?> GetUploadUrlAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("files/upload_url", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FileUploadUrlResponse>(cancellationToken: cancellationToken);
        return result?.UploadUrl;
    }

    private async Task<string?> UploadFileAsync(string uploadUrl, Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        using var multipart = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Add the file under the field name "file"
        multipart.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync(uploadUrl, multipart, cancellationToken);

        // Check for success, but also log the content on failure for debugging
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to upload file to VirusTotal. Status: {statusCode}. Response: {response}", response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode(); // This will now throw a detailed exception
        }

        // The response contains the analysis ID
        var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>(cancellationToken: cancellationToken);

        // Return the analysis ID if the type is "analysis" and an ID exists.
        return (result?.Data?.Type == "analysis" && !string.IsNullOrEmpty(result.Data.Id))
            ? result.Data.Id
            : null;
    }

    private async Task<ScanResult> PollForCompletionAsync(string analysisId, CancellationToken cancellationToken)
    {
        for (int i = 0; i < MAX_POLLING_ATTEMPTS; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _httpClient.GetAsync($"analyses/{analysisId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get analysis report for ID '{analysisId}'. Status: {statusCode}", analysisId, response.StatusCode);
                await Task.Delay(POLLING_INTERVAL_MS, cancellationToken);
                continue;
            }

            var report = await response.Content.ReadFromJsonAsync<AnalysisReportResponse>(cancellationToken: cancellationToken);
            var status = report?.Data?.Attributes?.Status;

            if (status == "completed")
            {
                var stats = report?.Data?.Attributes?.Stats;
                _logger.LogInformation("Scan completed for ID '{analysisId}'. Malicious: {malicious}, Suspicious: {suspicious}",
                    analysisId, stats?.Malicious, stats?.Suspicious);

                if (stats != null && stats.Malicious > 0)
                {
                    return new ScanResult(ScanResultStatus.Malicious, $"{stats.Malicious} engines detected malware.");
                }
                return new ScanResult(ScanResultStatus.Safe, "No malware detected.");
            }

            _logger.LogInformation("Scan status for '{analysisId}' is '{status}'. Waiting...", analysisId, status);
            await Task.Delay(POLLING_INTERVAL_MS, cancellationToken);
        }

        _logger.LogWarning("Scan timed out for analysis ID '{analysisId}'.", analysisId);
        return new ScanResult(ScanResultStatus.Timeout, "Scan did not complete within the time limit.");
    }
}