using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly ApplicationDbContext _context;

    public AppUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.AppUsers.FindAsync(new object[] { id }, cancellationToken);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.AppUsers.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        => await _context.AppUsers.AddAsync(user, cancellationToken);

    public Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _context.AppUsers.Update(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
