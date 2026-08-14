using Draya.Application.Materials;
using Draya.Application.Materials.Notifications;
using Draya.Domain.Materials;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Materials;

public class MaterialProcessingBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaterialProcessingBackgroundService> _logger;

    public MaterialProcessingBackgroundService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<MaterialProcessingBackgroundService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Material Processing Background Service is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await ProcessItemAsync(workItem, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing material id {MaterialId}", workItem.MaterialId);
            }
        }
    }

    private async Task ProcessItemAsync(MaterialProcessingItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var videoProvider = scope.ServiceProvider.GetRequiredService<IVideoProviderService>();
        var materialRepository = scope.ServiceProvider.GetRequiredService<IMaterialRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        try
        {
            var version = await materialRepository.GetVersionByIdAsync(item.VersionId);
            if (version == null)
            {
                _logger.LogWarning("Version {VersionId} not found for material {MaterialId}", item.VersionId, item.MaterialId);
                return;
            }

            var videoId = await videoProvider.UploadVideoAsync(item.FilePath, item.Title, "Uploaded via Draya API", cancellationToken);

            // Fetch the material to attach the VideoDetail
            var material = await materialRepository.GetByIdAsync(item.MaterialId);
            if (material != null)
            {
                var videoDetail = new VideoDetail
                {
                    MaterialId = material.Id,
                    DurationSeconds = 0, // We could fetch this later with another API call
                    Provider = "YouTube",
                    ProviderVideoId = videoId,
                    EmbedUrl = $"https://www.youtube.com/embed/{videoId}"
                };

                // Remove existing video details just in case it's a replace, or handle accordingly
                // (Assuming 1 VideoDetail per Material, or handled differently by business rules.
                // If it's 1-to-1 with Material, we add it or update it.)
                material.VideoDetail = videoDetail; 
            }

            version.ParseStatus = ParseStatus.Parsed;
            await materialRepository.SaveChangesAsync();

            // Notify clients via MediatR
            await publisher.Publish(new MaterialParsedNotification(item.MaterialId, item.VersionId, "Parsed"), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video to YouTube.");
            var version = await materialRepository.GetVersionByIdAsync(item.VersionId);
            if (version != null)
            {
                version.ParseStatus = ParseStatus.Failed;
                version.ParseErrorMessage = ex.Message;
                await materialRepository.SaveChangesAsync(cancellationToken);
                await publisher.Publish(new MaterialParsedNotification(item.MaterialId, item.VersionId, "Failed", ex.Message), cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(item.FilePath))
            {
                try
                {
                    File.Delete(item.FilePath);
                    _logger.LogInformation("Deleted temporary file {FilePath}", item.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary file {FilePath}", item.FilePath);
                }
            }
        }
    }
}
