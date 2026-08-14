using Draya.Application.Materials.DTOs;
using Draya.Domain.Materials;

namespace Draya.Application.Materials;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IBackgroundTaskQueue _taskQueue;

    public MaterialService(IMaterialRepository materialRepository, IBlobStorageService blobStorageService, IBackgroundTaskQueue taskQueue)
    {
        _materialRepository = materialRepository;
        _blobStorageService = blobStorageService;
        _taskQueue = taskQueue;
    }

    public async Task<MaterialDto> UploadLessonMaterialAsync(Guid classroomId, string title, string materialType, Stream fileStream, string fileName, string contentType)
    {
        var type = Enum.Parse<MaterialType>(materialType, true);

        var material = new LearningMaterial
        {
            ClassroomId = classroomId,
            Title = title,
            MaterialType = type
        };

        var version = new MaterialVersion
        {
            MaterialId = material.Id,
            VersionNumber = 1,
            ParseStatus = ParseStatus.Pending
        };

        var fileUrl = string.Empty;

        if (type == MaterialType.Video)
        {
            var tempPath = Path.GetTempFileName();
            using (var fileStreamDest = new FileStream(tempPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamDest);
            }
            
            await _materialRepository.AddAsync(material);

            await _taskQueue.QueueBackgroundWorkItemAsync(new MaterialProcessingItem(
                material.Id, version.Id, tempPath, title, contentType));
        }
        else
        {
            fileUrl = await _blobStorageService.UploadFileAsync("materials", $"{material.Id}/{version.Id}_{fileName}", fileStream, contentType);
            version.FileUrl = fileUrl;
            version.ParseStatus = ParseStatus.Parsed; // Documents are parsed/stored immediately for now
            material.Versions.Add(version);
            await _materialRepository.AddAsync(material);
        }

        return MapToDto(material);
    }

    public async Task<IEnumerable<MaterialDto>> GetClassroomMaterialsAsync(Guid classroomId, int page, int pageSize)
    {
        var materials = await _materialRepository.GetByClassroomIdAsync(classroomId, page, pageSize);
        return materials.Select(MapToDto);
    }

    public async Task<MaterialDto> GetMaterialDetailAsync(Guid materialId)
    {
        var material = await _materialRepository.GetByIdAsync(materialId);
        if (material == null) throw new Exception("Material not found");

        return MapToDto(material);
    }

    public async Task<MaterialVersionDto> UploadNewMaterialVersionAsync(Guid materialId, Stream fileStream, string fileName, string contentType)
    {
        var material = await _materialRepository.GetByIdAsync(materialId);
        if (material == null) throw new Exception("Material not found");

        var latestVersionNumber = material.Versions.Any() ? material.Versions.Max(v => v.VersionNumber) : 0;

        var version = new MaterialVersion
        {
            MaterialId = material.Id,
            VersionNumber = latestVersionNumber + 1,
            ParseStatus = ParseStatus.Pending
        };

        var fileUrl = string.Empty;

        if (material.MaterialType == MaterialType.Video)
        {
            var tempPath = Path.GetTempFileName();
            using (var fileStreamDest = new FileStream(tempPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamDest);
            }
            
            await _materialRepository.AddVersionAsync(version);

            await _taskQueue.QueueBackgroundWorkItemAsync(new MaterialProcessingItem(
                material.Id, version.Id, tempPath, fileName, contentType));
        }
        else
        {
            fileUrl = await _blobStorageService.UploadFileAsync("materials", $"{material.Id}/{version.Id}_{fileName}", fileStream, contentType);
            version.FileUrl = fileUrl;
            version.ParseStatus = ParseStatus.Parsed;
            await _materialRepository.AddVersionAsync(version);
        }

        return MapToVersionDto(version);
    }

    public async Task<IEnumerable<MaterialVersionDto>> GetMaterialVersionHistoryAsync(Guid materialId)
    {
        var versions = await _materialRepository.GetVersionsByMaterialIdAsync(materialId);
        return versions.Select(MapToVersionDto);
    }

    public async Task<object> GetVersionParseStatusAsync(Guid materialId, Guid versionId)
    {
        var version = await _materialRepository.GetVersionByIdAsync(versionId);
        if (version == null) throw new Exception("Version not found");

        return new
        {
            versionId = version.Id,
            parseStatus = version.ParseStatus.ToString(),
            errorMessage = version.ParseErrorMessage
        };
    }

    public async Task DeleteMaterialAsync(Guid materialId)
    {
        var material = await _materialRepository.GetByIdAsync(materialId);
        if (material == null) throw new Exception("Material not found");

        await _materialRepository.DeleteAsync(material);
    }

    public async Task<VideoStreamDto> GetVideoStreamingUrlAsync(Guid materialId)
    {
        var material = await _materialRepository.GetByIdAsync(materialId);
        if (material == null) throw new Exception("Material not found");
        if (material.MaterialType != MaterialType.Video) throw new Exception("Material is not a video");

        var currentVersion = material.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (currentVersion == null) throw new Exception("Video file not found");

        var videoDetail = material.VideoDetail;
        if (videoDetail == null || string.IsNullOrEmpty(videoDetail.EmbedUrl))
        {
            throw new Exception("Video processing is not yet complete or video details are missing.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(2); // Provide a standard expiration conceptually

        return new VideoStreamDto
        {
            Provider = videoDetail.Provider,
            VideoId = videoDetail.ProviderVideoId ?? string.Empty,
            StreamUrl = videoDetail.EmbedUrl, // Reusing StreamUrl field to return EmbedUrl to frontend seamlessly
            ExpiresAt = expiresAt
        };
    }

    private static MaterialDto MapToDto(LearningMaterial material)
    {
        var currentVersion = material.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        return new MaterialDto
        {
            MaterialId = material.Id,
            Title = material.Title,
            MaterialType = material.MaterialType.ToString(),
            CreatedAt = material.CreatedAt,
            CurrentVersion = currentVersion != null ? MapToVersionDto(currentVersion) : null!
        };
    }

    private static MaterialVersionDto MapToVersionDto(MaterialVersion version)
    {
        return new MaterialVersionDto
        {
            VersionId = version.Id,
            VersionNumber = version.VersionNumber,
            FileUrl = version.FileUrl,
            ParseStatus = version.ParseStatus.ToString(),
            UploadedAt = version.UploadedAt,
            ErrorMessage = version.ParseErrorMessage
        };
    }
}
