using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record SubmitNewVersionCommand(
     Guid DeveloperId,
     Guid AppId,
     Guid SubmissionId,
     string VersionNumber,
     string? Changelog,
     string MainAppFileName,
     string MainAppChecksum,
     long MainAppFileSize,
     IReadOnlyList<ScreenshotInfo> Screenshots,
     IReadOnlyList<string> Tags) : IRequest<Guid>;
}
