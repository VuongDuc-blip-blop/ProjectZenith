using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record SubmitReviewReplyCommand(Guid DeveloperId, Guid ReviewId, string ReplyContent) : IRequest<Unit>;
}
