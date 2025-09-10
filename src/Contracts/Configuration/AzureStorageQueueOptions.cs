namespace ProjectZenith.Contracts.Configuration
{
    public class AzureStorageQueueOptions
    {
        public const string SectionName = "Queue";
        public string PayoutsToProcessQueue { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string DeveloperStatusReconciliationQueue { get; set; } = string.Empty;
    }
}
