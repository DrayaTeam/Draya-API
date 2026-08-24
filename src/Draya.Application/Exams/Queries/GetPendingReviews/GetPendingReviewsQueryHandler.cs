using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Classrooms.DTOs;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Exams.Queries.GetPendingReviews;

public class GetPendingReviewsQueryHandler : IRequestHandler<GetPendingReviewsQuery, PagedResult<PendingReviewClassroomDto>>
{
    private readonly IStudentExamAttemptRepository _attemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentRepository _studentRepository;

    public GetPendingReviewsQueryHandler(
        IStudentExamAttemptRepository attemptRepository,
        IExamRepository examRepository,
        ISectionRepository sectionRepository,
        IClassroomRepository classroomRepository,
        IStudentRepository studentRepository)
    {
        _attemptRepository = attemptRepository;
        _examRepository = examRepository;
        _sectionRepository = sectionRepository;
        _classroomRepository = classroomRepository;
        _studentRepository = studentRepository;
    }

    public async Task<PagedResult<PendingReviewClassroomDto>> Handle(GetPendingReviewsQuery request, CancellationToken cancellationToken)
    {
        var attemptsQuery = _attemptRepository.GetQueryable();
        var examsQuery = _examRepository.GetQueryable();
        var sectionsQuery = _sectionRepository.GetQueryable();
        var classroomsQuery = _classroomRepository.GetQueryable();
        var studentsQuery = _studentRepository.GetQueryable();

        // 1. Get all classroom IDs with pending reviews for this teacher
        var classroomIdsQuery = (from attempt in attemptsQuery
                                 where attempt.NeedsTeacherReview && attempt.IsSubmitted
                                 join exam in examsQuery on attempt.ExamId equals exam.Id
                                 join section in sectionsQuery on exam.SectionId equals section.Id
                                 join classroom in classroomsQuery on section.ClassroomId equals classroom.Id
                                 where classroom.TeacherId == request.TeacherId
                                 select classroom.Id).Distinct();

        var totalCount = classroomIdsQuery.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        // 2. Paginate the classrooms
        var paginatedClassroomIds = classroomIdsQuery
            .OrderBy(id => id) // Ensure stable ordering
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // 3. Fetch the data only for the paginated classrooms
        var query = from attempt in attemptsQuery
                    where attempt.NeedsTeacherReview && attempt.IsSubmitted
                    join exam in examsQuery on attempt.ExamId equals exam.Id
                    join section in sectionsQuery on exam.SectionId equals section.Id
                    join classroom in classroomsQuery on section.ClassroomId equals classroom.Id
                    where paginatedClassroomIds.Contains(classroom.Id)
                    join student in studentsQuery on attempt.StudentId equals student.UserId
                    select new 
                    {
                        ClassroomId = classroom.Id,
                        ClassroomName = classroom.Name,
                        ExamId = exam.Id,
                        ExamTitle = exam.Title,
                        AttemptId = attempt.Id,
                        StudentId = student.UserId,
                        StudentName = student.FullName,
                        SubmittedAt = attempt.SubmittedAt,
                        Score = attempt.FinalScore
                    };

        var rawData = query.ToList();

        var result = rawData
            .GroupBy(x => new { x.ClassroomId, x.ClassroomName })
            .Select(cGroup => new PendingReviewClassroomDto(
                cGroup.Key.ClassroomId,
                cGroup.Key.ClassroomName,
                cGroup.GroupBy(x => new { x.ExamId, x.ExamTitle })
                      .Select(eGroup => new PendingReviewExamDto(
                          eGroup.Key.ExamId,
                          eGroup.Key.ExamTitle,
                          eGroup.Select(a => new PendingReviewAttemptDto(
                              a.AttemptId,
                              a.StudentId,
                              a.StudentName,
                              a.SubmittedAt,
                              a.Score
                          )).ToList()
                      )).ToList()
            )).ToList();

        return new PagedResult<PendingReviewClassroomDto>(
            result,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
