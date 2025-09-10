using MediatR;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
         where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly WriteDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            WriteDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            Guid? userId = null;
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            _logger.LogInformation("Handling command {RequestName} by User {UserId} with data: {@Request}",
                requestName, userId, request);

            try
            {
                var response = await next();

                var logEntry = new SystemLog
                {
                    UserId = userId,
                    Action = requestName,
                    Details = "Command executed successfully.",
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                _dbContext.SystemLogs.Add(logEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                var logEntry = new SystemLog
                {
                    UserId = userId,
                    Action = requestName,
                    Details = $"Command failed: {ex.Message}",
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.UtcNow
                };
                _dbContext.SystemLogs.Add(logEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogError(ex, "Error handling command {RequestName} by User {UserId}", requestName, userId);
                throw;
            }
        }
    }
}
