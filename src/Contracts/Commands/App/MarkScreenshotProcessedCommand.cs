using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record MarkScreenshotProcessedCommand(Guid AppId, Guid ScreenshotId, string BlobName, string Checksum) : IRequest<Unit>;
}
