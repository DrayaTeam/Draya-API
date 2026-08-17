using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Repositories;

public class PlatformAdminRepository : IPlatformAdminRepository
{
    private readonly ApplicationDbContext _context;

    public PlatformAdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformAdmin?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.PlatformAdmins.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<List<PlatformAdmin>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.PlatformAdmins.ToListAsync(cancellationToken);

    public async Task AddAsync(PlatformAdmin admin, CancellationToken cancellationToken = default)
        => await _context.PlatformAdmins.AddAsync(admin, cancellationToken);

    public Task UpdateAsync(PlatformAdmin admin, CancellationToken cancellationToken = default)
    {
        _context.PlatformAdmins.Update(admin);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
