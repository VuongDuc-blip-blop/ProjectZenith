using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record UnpublishVersionCommand(Guid AppId, Guid VersionId) : IRequest<Unit>;
}
