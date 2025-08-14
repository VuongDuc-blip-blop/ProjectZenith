using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public class UserLoggedInEvent
    {

        public Guid UserId { get; init; }

        [Required]
        [EmailAddress]
        public string Email { get; init; }

        [DataType(DataType.DateTime)]
        public DateTime LoggedInAt { get; init; }
    }
}
