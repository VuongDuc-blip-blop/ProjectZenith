using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands;

namespace ProjectZenith.Api.Write.Validation.User
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        private readonly WriteDbContext _context;
        public RegisterUserCommandValidator(WriteDbContext dbContext)
        {
            _context = dbContext;

            //Rule 1: Email must be a valid email format, not empty and unique in the database.
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MustAsync(BeUniqueEmail).WithMessage("Email already exists.");

            //Rule 2: Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, and one number.
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$")
                .WithMessage("Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, and one number.");

            //Rule 3: Username must be at most 100 characters long and can be null.
            RuleFor(x => x.Username)
                .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.")
                .When(x => x.Username != null)
                .MustAsync(BeUniqueUsername).WithMessage("Username already exists.");
        }

        public async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }
        public async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
        }
    }
}
