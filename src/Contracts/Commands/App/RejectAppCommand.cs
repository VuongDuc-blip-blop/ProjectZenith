using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record RejectAppCommand(Guid AppId, Guid AppFileId, string Reason, string RejectedPath) : IRequest<Unit>;
}
