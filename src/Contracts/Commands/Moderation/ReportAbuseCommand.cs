using MediatR;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Contracts.Commands.Moderation
{
    public record ReportAbuseCommand(Guid ReporterId, ReportableContentType TargetType, Guid TargetId, string Reason) : IRequest<Guid>;
}
