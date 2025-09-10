using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Api.Write.Services.AppDomain.DomainServices
{
    public class AppStatusService : IAppStatusService
    {
        private readonly WriteDbContext _dbContext;
        private readonly ILogger<AppStatusService> _logger;

        public AppStatusService(WriteDbContext dbContext, ILogger<AppStatusService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task UpdateAppStatusAsync(Guid appId, CancellationToken cancellationToken)
        {
            var app = await _dbContext.Apps
                .Include(a => a.Versions)
                .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {appId} not found.");

            var hasPublishedVersion = app.Versions.Any(v => v.Status == Status.Published);

            var hasValidPrice = app.Price > 0;

            var newStatus = (hasPublishedVersion && hasValidPrice) ? AppStatus.Active : AppStatus.Delisted;

            if (app.AppStatus != newStatus)
            {
                _logger.LogInformation("Updating status for app {AppId} from {OldStatus} to {NewStatus}", appId, app.AppStatus, newStatus);
                app.AppStatus = newStatus;
                app.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update status for app {AppId}: {Message}", appId, ex.Message);
                    throw;
                }
            }

        }
    }
}
