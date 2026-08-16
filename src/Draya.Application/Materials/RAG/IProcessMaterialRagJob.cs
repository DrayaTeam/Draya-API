using Draya.Domain.Materials;

namespace Draya.Application.Materials.RAG;

public interface IProcessMaterialRagJob
{
    Task ProcessAsync(LearningMaterial material, MaterialVersion version, string filePath, CancellationToken ct);
}
