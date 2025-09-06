namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Options for the developer.
    /// </summary>
    public class DeveloperOptions
    {
        /// <summary>
        /// The approval policy for user requests to be a developer.
        /// </summary>
        public string ApprovalPolicy { get; set; } = "Admin"; // Default to "Admin"
    }
}
