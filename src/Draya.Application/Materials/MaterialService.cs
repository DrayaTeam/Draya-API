using Draya.Application.Materials.DTOs;
using Draya.Domain.Materials;

namespace Draya.Application.Materials;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IBlobStorageService _blobStorageService;

    public MaterialService(IMaterialRepository materialRepository, IBlobStorageService blobStorageService)
    {
        _materialRepository = materialRepository;
        _blobStorageService = blobStorageService;
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

        var fileUrl = await _blobStorageService.UploadFileAsync("materials", $"{material.Id}/{version.Id}_{fileName}", fileStream, contentType);
        version.FileUrl = fileUrl;

        material.Versions.Add(version);

        await _materialRepository.AddAsync(material);

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

        var fileUrl = await _blobStorageService.UploadFileAsync("materials", $"{material.Id}/{version.Id}_{fileName}", fileStream, contentType);
        version.FileUrl = fileUrl;

        await _materialRepository.AddVersionAsync(version);

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

        // Assuming FileUrl is a direct blob URL, we extract blob name from it
        var uri = new Uri(currentVersion.FileUrl);
        var blobName = uri.Segments[^2] + uri.Segments[^1]; // simple heuristic if format is container/folder/file

        // In practice we'd just use a robust parse, let's keep it simple for the PoC
        // if fileUrl is like https://acc.blob.core.windows.net/materials/guid/guid_name.mp4
        var path = uri.AbsolutePath.TrimStart('/'); // materials/guid/guid_name.mp4
        var containerName = path.Split('/')[0];
        var actualBlobName = path.Substring(containerName.Length + 1);

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var streamUrl = _blobStorageService.GetServiceSasUriForBlob(containerName, actualBlobName, expiresAt);

        return new VideoStreamDto
        {
            StreamUrl = streamUrl,
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
