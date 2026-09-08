using Draya.Application.Classrooms.DTOs;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Exams.Queries.GetStudentExams;

public class GetStudentExamsQueryHandler : IRequestHandler<GetStudentExamsQuery, PagedResult<StudentExamSummaryDto>>
{
    private readonly IExamRepository _examRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentExamAttemptRepository _attemptRepository;
    private readonly IExamGenerationRepository _generationRepo;

    public GetStudentExamsQueryHandler(
        IExamRepository examRepository, 
        IClassroomRepository classroomRepository, 
        IStudentExamAttemptRepository attemptRepository,
        IExamGenerationRepository generationRepo)
    {
        _examRepository = examRepository;
        _classroomRepository = classroomRepository;
        _attemptRepository = attemptRepository;
        _generationRepo = generationRepo;
    }

    public async Task<PagedResult<StudentExamSummaryDto>> Handle(GetStudentExamsQuery request, CancellationToken cancellationToken)
    {
        var classroomIds = await _classroomRepository.GetEnrolledClassroomIdsAsync(request.StudentId, cancellationToken);
        var myPracticeExamIds = await _generationRepo.GetPracticeExamIdsByStudentAsync(request.StudentId, cancellationToken);

        var queryable = _examRepository.GetQueryable()
            .Where(e => classroomIds.Contains(e.ClassroomId) 
                     && (!e.Title.StartsWith("Practice Mini-Exam:") || myPracticeExamIds.Contains(e.Id)));

        var totalCount = queryable.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var exams = queryable
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var examIds = exams.Select(e => e.Id).ToList();
        var attempts = await _attemptRepository.GetAttemptsByStudentAndExamsAsync(request.StudentId, examIds, cancellationToken);
        var attemptsByExam = attempts.GroupBy(a => a.ExamId).ToDictionary(g => g.Key, g => g.ToList());

        var items = exams.Select(e =>
        {
            var examAttempts = attemptsByExam.TryGetValue(e.Id, out var list) ? list : new List<StudentExamAttempt>();
            var usedAttempts = examAttempts.Count;
            var latestAttempt = examAttempts.OrderByDescending(a => a.StartedAt).FirstOrDefault();
            
            bool hasSubmitted = latestAttempt?.IsSubmitted ?? false;
            
            string attemptStatus = "NotStarted";
            if (latestAttempt != null)
            {
                if (latestAttempt.FinalScore.HasValue) attemptStatus = "Completed";
                else if (latestAttempt.IsSubmitted) attemptStatus = "PendingGrading";
                else attemptStatus = "InProgress";
            }

            decimal? latestScore = latestAttempt?.FinalScore;
            
            var mappedAttempts = examAttempts.Select(a => new StudentExamAttemptSummaryDto(
                a.Id,
                a.FinalScore,
                a.MaxScore,
                a.NeedsTeacherReview,
                a.SubmittedAt,
                a.StartedAt
            )).ToList();

            return new StudentExamSummaryDto(
                e.Id,
                e.ClassroomId,
                e.SectionId,
                e.Title,
                e.Topic,
                e.DurationMinutes,
                e.StartDate,
                e.EndDate,
                e.AllowedAttempts,
                e.CreatedAt,
                hasSubmitted,
                attemptStatus,
                latestScore,
                latestAttempt?.MaxScore,
                usedAttempts,
                mappedAttempts
            );
        }).ToList();

        return new PagedResult<StudentExamSummaryDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
