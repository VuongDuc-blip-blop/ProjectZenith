using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record MarkVersionAsPendingApprovalCommand(Guid AppId, Guid VersionId, Guid AppFileId, string FinalPath) : IRequest<Unit>;
}
