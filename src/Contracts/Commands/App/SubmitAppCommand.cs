using MediatR;
using Microsoft.AspNetCore.Http;
using ProjectZenith.Contracts.Events.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.App
{
    public record SubmitAppCommand : IRequest<Guid>
    {
        public Guid DeveloperId { get; init; }
        [Required]
        public string Name { get; init; }
        [Required]
        public string Description { get; init; }
        [Required]
        public string Category { get; init; }
        [Required]
        public string Platform { get; init; }
        [Required]
        public decimal Price { get; init; }
        [Required]
        public string VersionNumber { get; init; }
        public string? Changelog { get; init; }
        public IFormFile AppFile { get; init; }

    }
}
