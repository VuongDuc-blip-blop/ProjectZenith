using ProjectZenith.Contracts.Interfaces;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents the many-to-many relationship between applications and tags.
    /// </summary>
    public class AppTag : ISoftDeletable
    {
        public Guid AppId { get; set; }
        public App App { get; set; } = null!;
        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
