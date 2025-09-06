using MediatR;
using Microsoft.AspNetCore.Http;
using ProjectZenith.Contracts.Events.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to update a user's avatar.
    /// </summary>
    public record UpdateUserAvatarCommand : IRequest<UserAvatarUpdatedEvent>
    {
        /// <summary>
        /// The unique identifier of the user whose avatar is to be updated.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The file containing the new avatar image.
        /// </summary>
        [Required]
        public IFormFile file { get; init; } = null!;
    }
}
