using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Options for configuring Azure Blob Storage.
    /// </summary>
    public class BlobStorageOptions
    {
        /// <summary>
        /// the name of the Azure Blob Storage account.
        /// </summary>
        public string AccountName { get; set; } = "projectzenithstorage100";

        [Required(AllowEmptyStrings = false)]
        public string QuarantineContainerName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string ValidatedContainerName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string PublishedContainerName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string RejectedContainerName { get; set; } = string.Empty;

        public string EventQueue { get; set; } = "app-events";
        public string AppFileQueue { get; set; } = "app-files-pending-validation";
        public string ScreenshotQueue { get; set; } = "screenshots-pending-validation";
        public int MaxScreenshots { get; set; } = 5;
        public long MaxScreenshotSize { get; set; } = 5 * 1024 * 1024; // 5 MB
        public int MaxTags { get; set; } = 10;

        // The name of the configuration section in local.settings.json
        public const string SectionName = "BlobStorage";
    }
}
