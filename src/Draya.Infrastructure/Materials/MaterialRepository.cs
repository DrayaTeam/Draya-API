using Draya.Domain.Materials;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Materials;

public class MaterialRepository : IMaterialRepository
{
    private readonly ApplicationDbContext _context;

    public MaterialRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LearningMaterial?> GetByIdAsync(Guid id)
    {
        return await _context.LearningMaterials
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<IEnumerable<LearningMaterial>> GetByClassroomIdAsync(Guid classroomId, int page, int pageSize)
    {
        return await _context.LearningMaterials
            .Include(m => m.Versions)
            .Where(m => m.ClassroomId == classroomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(LearningMaterial material)
    {
        await _context.LearningMaterials.AddAsync(material);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LearningMaterial material)
    {
        _context.LearningMaterials.Update(material);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(LearningMaterial material)
    {
        material.IsDeleted = true;
        _context.LearningMaterials.Update(material);
        await _context.SaveChangesAsync();
    }

    public async Task AddVersionAsync(MaterialVersion version)
    {
        await _context.MaterialVersions.AddAsync(version);
        await _context.SaveChangesAsync();
    }

    public async Task<MaterialVersion?> GetVersionByIdAsync(Guid versionId)
    {
        return await _context.MaterialVersions
            .FirstOrDefaultAsync(v => v.Id == versionId);
    }

    public async Task<IEnumerable<MaterialVersion>> GetVersionsByMaterialIdAsync(Guid materialId)
    {
        return await _context.MaterialVersions
            .Where(v => v.MaterialId == materialId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
}
