using Draya.Domain.Identity;

namespace Draya.Domain.Identity;

public interface IPlatformAdminRepository
{
    Task<PlatformAdmin?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<PlatformAdmin>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PlatformAdmin admin, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlatformAdmin admin, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
