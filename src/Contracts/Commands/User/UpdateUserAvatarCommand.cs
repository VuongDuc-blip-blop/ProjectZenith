using MediatR;
using Microsoft.AspNetCore.Http;
using ProjectZenith.Contracts.Events.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record UpdateUserAvatarCommand : IRequest<UserAvatarUpdatedEvent>
    {
        public Guid UserId { get; init; }

        [Required]
        public IFormFile file { get; init; } = null!;
    }
}
