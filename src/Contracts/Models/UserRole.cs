using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a user role in the system.
    /// A user role defines the association between a user and a role, allowing for role-based
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        [Key, Column(Order = 1)]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property to the user associated with this role.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// The unique identifier for the role.
        /// </summary>
        [Key, Column(Order = 2)]
        public Guid RoleId { get; set; }

        /// <summary>
        /// Navigation property to the role associated with this user.
        /// </summary>
        public Role Role { get; set; } = null!;
    }
}
