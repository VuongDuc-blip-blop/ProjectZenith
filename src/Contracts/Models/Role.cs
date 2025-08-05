using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a role in the system.
    /// A role defines a set of permissions and can be assigned to users.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// The unique identifier for the role.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the role.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Role name cannot be longer than 50 characters.")]
        public string Name { get; set; } = null!;

        public ICollection<UserRole> UsersOfRole { get; set; } = null!;
    }
}
