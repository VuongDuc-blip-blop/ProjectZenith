using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class SubmitReviewReplyCommandValidator : AbstractValidator<SubmitReviewReplyCommand>
    {
        public SubmitReviewReplyCommandValidator()
        {
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
            RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review ID is required.");
            RuleFor(x => x.ReplyContent).NotEmpty().WithMessage("Reply content is required.")
                                        .MaximumLength(1000).WithMessage("Reply cannot exceed 1000 characters.");
        }
    }

}
