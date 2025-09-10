using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record DeleteAppCommand(Guid AppId, Guid DeveloperId) : IRequest<Unit>;
}
