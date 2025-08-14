using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Events;
using ProjectZenith.Contracts.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class UpdateUserAvatarService
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;

        public UpdateUserAvatarService(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
        }

        public async Task<UserAvatarUpdatedEvent> HandleAsync(Guid UserId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file upload");
            }
            if (file.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 2MB limit");
            }

            string fileExetension;
            try
            {
                using var imageStream = file.OpenReadStream();
                IImageFormat format = await Image.DetectFormatAsync(imageStream);

                // Explicitly check if the format is one we allow
                if (format != PngFormat.Instance && format != JpegFormat.Instance)
                {
                    throw new NotSupportedException("Invalid file format. Only JPEG or PNG files are allowed.");
                }

                // Reset the stream before the next read operation
                imageStream.Position = 0;

                // ---STEP 4: Fully load the image to validate its integrity ---
                // This is our defense against corrupted files, decompression bombs, etc.
                // The 'using' statement ensures the image object is disposed correctly.
                using (Image image = await Image.LoadAsync(imageStream, cancellationToken))
                {
                    // If we get here, the image is valid.
                    // We can check dimensions if we want.
                    if (image.Width > 1024 || image.Height > 1024)
                    {
                        throw new ArgumentException("Image dimensions cannot exceed 1024x1024 pixels.");
                    }
                }

                // Determine the correct file extension for saving.
                fileExetension = format.Name.ToLowerInvariant() == "jpeg" ? ".jpg" : ".png";

            }
            catch (UnknownImageFormatException ex)
            {
                // catches files that are not images at all (e.g., a renamed .exe).
                throw new ArgumentException("The uploaded file is not a valid image.", ex);
            }
            catch (NotSupportedException ex)
            {
                // catches explicit check for non-PNG/JPEG formats.
                throw new ArgumentException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                // catch-all for other processing errors.
                throw new ArgumentException("There was an error processing the uploaded image.", ex);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            var fileName = $"{UserId}{fileExetension}";

            // SIMULATE BLOB STORAGE UPLOAD
            // var blobStorageService = new BlobStorageService();
            // var avatarUrl = await blobStorageService.UploadAvatarAsync(fileName, file.OpenReadStream());
            var avatarUrl = $"https://your-blob-storage-account.blob.core.windows.net/avatars/{fileName}";

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                user.AvatarUrl = avatarUrl;

                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "AvatarUpdate",
                    Details = $"User {user.Email} updated avatar to {avatarUrl}",
                    Timestamp = DateTime.UtcNow
                });

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException("Avatar update failed due to concurrent modification.");
                }

                var userEvent = new UserAvatarUpdatedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    AvatarUrl = avatarUrl,
                    UpdatedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return userEvent;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
