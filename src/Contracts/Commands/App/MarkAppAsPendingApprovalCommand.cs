using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record MarkAppAsPendingApprovalCommand(Guid AppId, Guid AppFileId, string FinalPath) : IRequest<Unit>;
}
