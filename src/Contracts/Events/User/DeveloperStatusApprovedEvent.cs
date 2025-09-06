namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's developer status is approved.
    /// </summary>
    public record DeveloperStatusApprovedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The date and time when the approval occurred.
        /// </summary>
        public DateTime ApprovedAt { get; init; }

        /// <summary>
        /// The unique identifier for the admin who approved the status.
        /// </summary>
        public Guid? ApprovedByAdminId { get; init; } = null;
    }
}
