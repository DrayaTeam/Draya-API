using Draya.Application.Common.Models;
using Draya.Application.Materials.DTOs;

namespace Draya.Application.Materials;

public interface IMaterialService
{
    Task<MaterialDto> UploadLessonMaterialAsync(Guid classroomId, string title, string materialType, Stream fileStream, string fileName, string contentType);
    Task<PaginatedResult<MaterialDto>> GetClassroomMaterialsAsync(Guid classroomId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedResult<MaterialDto>> GetStudentEnrolledMaterialsAsync(Guid studentId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MaterialDto> GetMaterialDetailAsync(Guid materialId);
    Task<MaterialVersionDto> UploadNewMaterialVersionAsync(Guid materialId, Stream fileStream, string fileName, string contentType);
    Task<IEnumerable<MaterialVersionDto>> GetMaterialVersionHistoryAsync(Guid materialId);
    Task<object> GetVersionParseStatusAsync(Guid materialId, Guid versionId);
    Task DeleteMaterialAsync(Guid materialId);
    Task<VideoStreamDto> GetVideoStreamingUrlAsync(Guid materialId);
}
