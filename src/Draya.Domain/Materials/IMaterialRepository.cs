using Draya.Domain.Materials;

namespace Draya.Domain.Materials;

public interface IMaterialRepository
{
    Task<LearningMaterial?> GetByIdAsync(Guid id);
    Task<(List<LearningMaterial> Items, int TotalCount)> GetByClassroomIdAsync(Guid classroomId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(List<LearningMaterial> Items, int TotalCount)> GetByClassroomIdsAsync(List<Guid> classroomIds, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(LearningMaterial material);

    Task UpdateAsync(LearningMaterial material);
    Task DeleteAsync(LearningMaterial material);
    Task AddVersionAsync(MaterialVersion version);
    Task<MaterialVersion?> GetVersionByIdAsync(Guid versionId);
    Task<IEnumerable<MaterialVersion>> GetVersionsByMaterialIdAsync(Guid materialId);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
