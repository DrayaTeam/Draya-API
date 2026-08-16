using Draya.Application.Materials;
using Draya.Application.Materials.Notifications;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using Draya.Domain.Materials;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
        var mediaStorageService = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();
        var materialRepository = scope.ServiceProvider.GetRequiredService<IMaterialRepository>();
        var classroomRepository = scope.ServiceProvider.GetRequiredService<IClassroomRepository>();
        var teacherRepository = scope.ServiceProvider.GetRequiredService<ITeacherRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        try
        {
            var version = await materialRepository.GetVersionByIdAsync(item.VersionId);
            if (version == null)
            {
                _logger.LogWarning("Version {VersionId} not found for material {MaterialId}", item.VersionId, item.MaterialId);
                return;
            }

            var material = await materialRepository.GetByIdAsync(item.MaterialId);
            var folderPath = $"teachers/Unknown/{material?.ClassroomId ?? Guid.Empty}";

            if (material != null)
            {
                try
                {
                    var classroom = await classroomRepository.GetByIdAsync(material.ClassroomId, cancellationToken);
                    if (classroom != null)
                    {
                        var teacher = await teacherRepository.GetByUserIdAsync(classroom.TeacherId, cancellationToken);
                        var teacherName = SanitizeFolderName(teacher?.FullName ?? $"Teacher_{classroom.TeacherId}");
                        var classroomName = SanitizeFolderName(classroom.Name);
                        folderPath = $"teachers/{teacherName}/{classroomName}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve Teacher/Classroom folder names for Material {MaterialId}", item.MaterialId);
                }
            }

            // Strip the GUID prefix so the filename in Cloudinary is just the clean original name
            var assetPath = $"{folderPath}/{item.FileName}";

            using (var fileStream = new FileStream(item.FilePath, FileMode.Open, FileAccess.Read))
            {
                var metadata = await mediaStorageService.UploadAsync(
                    fileStream, 
                    assetPath, 
                    item.ContentType ?? "video/mp4", 
                    cancellationToken);

                version.Provider        = metadata.Provider;
                version.ProviderAssetId = metadata.ProviderAssetId;
                version.SecureUrl       = metadata.SecureUrl;
                version.ResourceType    = metadata.ResourceType;
                version.Format          = metadata.Format;
            }

            if (material != null)
            {
                // Ensure we don't insert a duplicate VideoDetail if one already exists
                var dbContext = scope.ServiceProvider.GetRequiredService<Draya.Infrastructure.Persistence.ApplicationDbContext>();
                var existingVideoDetail = await dbContext.VideoDetails.FirstOrDefaultAsync(v => v.MaterialId == material.Id, cancellationToken);
                
                if (existingVideoDetail == null)
                {
                    var videoDetail = new VideoDetail
                    {
                        MaterialId = material.Id,
                        DurationSeconds = 0
                    };
                    dbContext.VideoDetails.Add(videoDetail); 
                }
            }

            version.ParseStatus = ParseStatus.Parsed;
            await materialRepository.SaveChangesAsync(cancellationToken);

            // Notify clients via MediatR
            await publisher.Publish(new MaterialParsedNotification(item.MaterialId, item.VersionId, "Parsed"), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video for Material {MaterialId}", item.MaterialId);
            
            // Use a fresh scope to save the error status, avoiding any faulted DbContext state
            using var errorScope = _serviceProvider.CreateScope();
            var errorRepo = errorScope.ServiceProvider.GetRequiredService<IMaterialRepository>();
            var errorPublisher = errorScope.ServiceProvider.GetRequiredService<IPublisher>();
            
            var version = await errorRepo.GetVersionByIdAsync(item.VersionId);
            if (version != null)
            {
                version.ParseStatus = ParseStatus.Failed;
                version.ParseErrorMessage = ex.Message;
                await errorRepo.SaveChangesAsync(cancellationToken);
                await errorPublisher.Publish(new MaterialParsedNotification(item.MaterialId, item.VersionId, "Failed", ex.Message), cancellationToken);
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

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "General";
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToArray();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        sanitized = sanitized.Replace(" ", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "General" : sanitized;
    }
}
