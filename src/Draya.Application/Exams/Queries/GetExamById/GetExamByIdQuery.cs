using System;
using Draya.Application.Exams.DTOs;
using MediatR;

namespace Draya.Application.Exams.Queries.GetExamById;

public record GetExamByIdQuery(Guid Id) : IRequest<ExamDto?>;
