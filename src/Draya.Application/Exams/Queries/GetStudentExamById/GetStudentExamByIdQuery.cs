using System;
using Draya.Application.Exams.DTOs;
using MediatR;

namespace Draya.Application.Exams.Queries.GetStudentExamById;

public record GetStudentExamByIdQuery(Guid Id) : IRequest<StudentExamDto?>;
