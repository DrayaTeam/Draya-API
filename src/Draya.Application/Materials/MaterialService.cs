using Draya.Application.Common.Models;
using Draya.Application.Materials.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using Draya.Domain.Materials;

namespace Draya.Application.Materials;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ITeacherRepository _teacherRepository;

    public MaterialService(
        IMaterialRepository materialRepository, 
        IMediaStorageService mediaStorageService, 
        IBackgroundTaskQueue taskQueue,
        IClassroomRepository classroomRepository,
        ITeacherRepository teacherRepository)
    {
        _materialRepository = materialRepository;
        _mediaStorageService = mediaStorageService;
        _taskQueue = taskQueue;
        _classroomRepository = classroomRepository;
        _teacherRepository = teacherRepository;
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

        if (type == MaterialType.Video)
        {
            var tempPath = Path.GetTempFileName();
            using (var fileStreamDest = new FileStream(tempPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamDest);
            }
            
            material.Versions.Add(version);
            await _materialRepository.AddAsync(material);

            await _taskQueue.QueueBackgroundWorkItemAsync(new MaterialProcessingItem(
                material.Id, version.Id, tempPath, title, contentType));
        }
        else
        {
            var folderPath = await GetCloudinaryFolderPathAsync(classroomId);
            var assetPath = $"{folderPath}/{material.Id}_{version.Id}_{fileName}";

            var metadata = await _mediaStorageService.UploadAsync(fileStream, assetPath, contentType);
            version.Provider = metadata.Provider;
            version.ProviderAssetId = metadata.ProviderAssetId;
            version.ResourceType = metadata.ResourceType;
            version.Format = metadata.Format;
            version.ParseStatus = ParseStatus.Parsed; // Documents are parsed/stored immediately for now
            material.Versions.Add(version);
            await _materialRepository.AddAsync(material);
        }

        return MapToDto(material);
    }

    public async Task<PaginatedResult<MaterialDto>> GetClassroomMaterialsAsync(Guid classroomId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (materials, totalCount) = await _materialRepository.GetByClassroomIdAsync(classroomId, page, pageSize, cancellationToken);
        var dtos = materials.Select(MapToDto).ToList();
        return new PaginatedResult<MaterialDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<PaginatedResult<MaterialDto>> GetStudentEnrolledMaterialsAsync(Guid studentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var enrolledClassroomIds = await _classroomRepository.GetEnrolledClassroomIdsAsync(studentId, cancellationToken);
        if (enrolledClassroomIds == null || !enrolledClassroomIds.Any())
        {
            return new PaginatedResult<MaterialDto>(new List<MaterialDto>(), 0, page, pageSize);
        }

        var (materials, totalCount) = await _materialRepository.GetByClassroomIdsAsync(enrolledClassroomIds, page, pageSize, cancellationToken);
        var dtos = materials.Select(MapToDto).ToList();
        return new PaginatedResult<MaterialDto>(dtos, totalCount, page, pageSize);
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
            var folderPath = await GetCloudinaryFolderPathAsync(material.ClassroomId);
            var assetPath = $"{folderPath}/{material.Id}_{version.Id}_{fileName}";

            var metadata = await _mediaStorageService.UploadAsync(fileStream, assetPath, contentType);
            version.Provider = metadata.Provider;
            version.ProviderAssetId = metadata.ProviderAssetId;
            version.ResourceType = metadata.ResourceType;
            version.Format = metadata.Format;
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

        if (string.IsNullOrEmpty(currentVersion.ProviderAssetId))
        {
            throw new Exception("Video processing is not yet complete or video details are missing.");
        }

        var metadata = new UploadedMediaMetadata
        {
            Provider = currentVersion.Provider ?? string.Empty,
            ProviderAssetId = currentVersion.ProviderAssetId,
            ResourceType = currentVersion.ResourceType ?? string.Empty,
            Format = currentVersion.Format ?? string.Empty
        };

        var streamUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);

        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

        return new VideoStreamDto
        {
            Provider = currentVersion.Provider ?? string.Empty,
            VideoId = currentVersion.ProviderAssetId,
            StreamUrl = streamUrl, 
            ExpiresAt = expiresAt
        };
    }

    private async Task<string> GetCloudinaryFolderPathAsync(Guid classroomId)
    {
        try
        {
            var classroom = await _classroomRepository.GetByIdAsync(classroomId);
            if (classroom != null)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(classroom.TeacherId);
                var teacherName = SanitizeFolderName(teacher?.FullName ?? $"Teacher_{classroom.TeacherId}");
                var classroomName = SanitizeFolderName(classroom.Name);
                return $"{teacherName}/{classroomName}";
            }
        }
        catch
        {
            // Fallback gracefully if classroom/teacher info isn't resolvable
        }
        return $"Classrooms/{classroomId}";
    }

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "General";
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToArray();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        sanitized = sanitized.Replace(" ", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "General" : sanitized;
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
            FileUrl = version.ProviderAssetId,
            ParseStatus = version.ParseStatus.ToString(),
            UploadedAt = version.UploadedAt,
            ErrorMessage = version.ParseErrorMessage
        };
    }
}
