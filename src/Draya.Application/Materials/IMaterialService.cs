using Draya.Application.Materials.DTOs;

namespace Draya.Application.Materials;

public interface IMaterialService
{
    Task<MaterialDto> UploadLessonMaterialAsync(Guid classroomId, string title, string materialType, Stream fileStream, string fileName, string contentType);
    Task<IEnumerable<MaterialDto>> GetClassroomMaterialsAsync(Guid classroomId, int page, int pageSize);
    Task<MaterialDto> GetMaterialDetailAsync(Guid materialId);
    Task<MaterialVersionDto> UploadNewMaterialVersionAsync(Guid materialId, Stream fileStream, string fileName, string contentType);
    Task<IEnumerable<MaterialVersionDto>> GetMaterialVersionHistoryAsync(Guid materialId);
    Task<object> GetVersionParseStatusAsync(Guid materialId, Guid versionId);
    Task DeleteMaterialAsync(Guid materialId);
    Task<VideoStreamDto> GetVideoStreamingUrlAsync(Guid materialId);
}
