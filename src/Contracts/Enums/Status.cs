namespace ProjectZenith.Contracts.Enums
{
    /// <summary>
    /// Represent the status of an application.
    /// </summary>
    public enum Status
    {
        Draft,
        ValidationFailed,
        PendingApproval,
        PendingValidation,
        Published,
        Rejected,
        Superseded,
        Archived,
        Banned
    }
}
