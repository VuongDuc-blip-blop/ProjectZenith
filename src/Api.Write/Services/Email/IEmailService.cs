namespace ProjectZenith.Api.Write.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a verification email with a secure token.
        /// </summary>
        /// <param name="email">The recipient's email address.</param>
        /// <param name="token">The verification token.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken);
    }
}
