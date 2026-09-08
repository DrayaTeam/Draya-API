using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class ClassroomFeedbackRepository : IClassroomFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public ClassroomFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid classroomId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.ClassroomFeedback
            .AnyAsync(f => f.ClassroomId == classroomId && f.StudentId == studentId, cancellationToken);
    }

    public async Task<ClassroomFeedback?> GetFeedbackAsync(Guid classroomId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.ClassroomFeedback
            .FirstOrDefaultAsync(f => f.ClassroomId == classroomId && f.StudentId == studentId, cancellationToken);
    }

    public async Task AddAsync(ClassroomFeedback feedback, CancellationToken cancellationToken = default)
    {
        await _context.ClassroomFeedback.AddAsync(feedback, cancellationToken);
    }

    public async Task<(List<ClassroomFeedback> Items, int TotalCount, double AverageRating)> GetByClassroomIdAsync(
        Guid classroomId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ClassroomFeedback
            .Where(f => f.ClassroomId == classroomId);

        var totalCount = await query.CountAsync(cancellationToken);
        var averageRating = totalCount == 0
            ? 0
            : await query.AverageAsync(f => (double)f.Rating, cancellationToken);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, averageRating);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
