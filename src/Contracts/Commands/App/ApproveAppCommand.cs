using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record ApproveAppCommand(Guid AppId, Guid VersionId) : IRequest<Unit>;
}
