

namespace Draya.Domain.Classrooms;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(List<Question> Items, int TotalCount)> GetClassroomQuestionsAsync(
        Guid classroomId,
        string sortBy,
        string filterBy,
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<(List<QuestionReply> Items, int TotalCount)> GetQuestionRepliesAsync(
        Guid questionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Question question, CancellationToken cancellationToken = default);
    Task UpdateAsync(Question question, CancellationToken cancellationToken = default);
    Task DeleteAsync(Question question, CancellationToken cancellationToken = default);

    Task AddReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default);
    Task UpdateReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default);
    Task DeleteReplyAsync(QuestionReply reply, CancellationToken cancellationToken = default);
    Task<QuestionReply?> GetReplyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Voting Operations
    Task AddVoteAsync(QuestionVote vote, CancellationToken cancellationToken = default);
    Task RemoveVoteAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasUserVotedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken = default);
}
