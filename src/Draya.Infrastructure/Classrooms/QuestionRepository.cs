
using Draya.Domain.Classrooms;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class QuestionRepository : IQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public QuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.Replies.OrderBy(r => r.CreatedAt))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<(List<Question> Items, int TotalCount)> GetClassroomQuestionsAsync(
        Guid classroomId,
        string sortBy,
        string filterBy,
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Questions.Where(q => q.ClassroomId == classroomId);

        // Filtering
        query = filterBy.ToLower() switch
        {
            "unanswered" => query.Where(q => !q.HasTeacherAnswer),
            "answered" => query.Where(q => q.HasTeacherAnswer),
            "myposts" => query.Where(q => q.AuthorId == currentUserId),
            _ => query // "all"
        };

        // Sorting
        query = sortBy.ToLower() switch
        {
            "mostvoted" => query.OrderByDescending(q => q.VoteCount).ThenByDescending(q => q.CreatedAt),
            "mostdiscussed" => query.OrderByDescending(q => q.ReplyCount).ThenByDescending(q => q.CreatedAt),
            // Simple deterministic trending score: (Votes * 2) + Replies + (1 / Days since creation)
            // Since EF Core can't easily translate complex date math in all providers, 
            // a simple translated way is using EF.Functions.DateDiffDay(q.CreatedAt, DateTime.UtcNow)
            // or just ordering by multiple fields as a proxy for trending in DB, then fallback.
            // Let's use a simpler proxy: order by (VoteCount * 2 + ReplyCount) DESC, then CreatedAt DESC
            "trending" => query.OrderByDescending(q => q.VoteCount * 2 + q.ReplyCount).ThenByDescending(q => q.CreatedAt),
            _ => query.OrderByDescending(q => q.CreatedAt) // "recent"
        };

        var count = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, count);
    }

    public async Task<(List<QuestionReply> Items, int TotalCount)> GetQuestionRepliesAsync(
        Guid questionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.QuestionReplies.Where(r => r.QuestionId == questionId)
                                            .OrderBy(r => r.CreatedAt);
        
        var count = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return (items, count);
    }

    public async Task AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _context.Questions.AddAsync(question, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Question question, CancellationToken cancellationToken = default)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Question question, CancellationToken cancellationToken = default)
    {
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default)
    {
        await _context.QuestionReplies.AddAsync(reply, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default)
    {
        _context.QuestionReplies.Update(reply);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default)
    {
        _context.QuestionReplies.Remove(reply);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuestionReply?> GetReplyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionReplies
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> HasTeacherReplyAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionReplies
            .AnyAsync(r => r.QuestionId == questionId && r.IsTeacherAnswer, cancellationToken);
    }

    public async Task AddVoteAsync(QuestionVote vote, CancellationToken cancellationToken = default)
    {
        await _context.QuestionVotes.AddAsync(vote, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var vote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId, cancellationToken);
            
        if (vote != null)
        {
            _context.QuestionVotes.Remove(vote);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasUserVotedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionVotes
            .AnyAsync(v => v.QuestionId == questionId && v.UserId == userId, cancellationToken);
    }
}
