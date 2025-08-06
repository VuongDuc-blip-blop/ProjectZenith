namespace ProjectZenith.Api.Write.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _baseUrl = "https://api.projectzenith.com/api/users/verify";

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }
        public async Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken)
        {
            // Mock implementation for development (replace with SendGrid in production)
            var verificationLink = $"{_baseUrl}?token={Uri.EscapeDataString(token)}";
            _logger.LogInformation("Sending verification email to {Email} with link: {Link}", email, verificationLink);
            await Task.CompletedTask; // Simulate async email sending
        }
    }
}
