namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents the many-to-many relationship between applications and tags.
    /// </summary>
    public class AppTag
    {
        public Guid AppId { get; set; }
        public App App { get; set; } = null!;
        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
