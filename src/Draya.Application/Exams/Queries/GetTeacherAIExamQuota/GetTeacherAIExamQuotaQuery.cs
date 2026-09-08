using System;
using Draya.Application.Exams.DTOs;
using MediatR;

namespace Draya.Application.Exams.Queries.GetTeacherAIExamQuota;

public record GetTeacherAIExamQuotaQuery(Guid TeacherId) : IRequest<AIExamQuotaDto>;
