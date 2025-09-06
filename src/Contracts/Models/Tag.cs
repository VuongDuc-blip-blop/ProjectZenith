using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a tag that can be associated with applications.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// The unique identifier for the tag.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the tag.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Tag name cannot be longer than 50 characters.")]
        public string Name { get; set; } = null!;
        public ICollection<AppTag> AppTags { get; set; } = new List<AppTag>();
    }
}
