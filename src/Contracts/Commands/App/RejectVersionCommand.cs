using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record RejectVersionCommand(Guid AppId, Guid VersionId, Guid AppFileId, string Reason, string RejectedPath) : IRequest<Unit>;
}
