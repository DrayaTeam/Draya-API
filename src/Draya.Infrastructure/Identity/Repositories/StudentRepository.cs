using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Identity.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _context;

    public StudentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Students.FindAsync(new object[] { userId }, cancellationToken);

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
        => await _context.Students.AddAsync(student, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
