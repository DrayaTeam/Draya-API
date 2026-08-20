using Draya.Application.Classrooms.DTOs;
using Draya.Application.Exams.DTOs;
using MediatR;
using System;

namespace Draya.Application.Exams.Queries.GetStudentExams;

public record GetStudentExamsQuery(
    Guid StudentId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<StudentExamSummaryDto>>;
