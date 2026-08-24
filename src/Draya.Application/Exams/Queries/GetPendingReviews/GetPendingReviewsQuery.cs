using System;
using System.Collections.Generic;
using Draya.Application.Exams.DTOs;
using MediatR;

using Draya.Application.Classrooms.DTOs;

namespace Draya.Application.Exams.Queries.GetPendingReviews;

public record GetPendingReviewsQuery(Guid TeacherId, int Page, int PageSize) : IRequest<PagedResult<PendingReviewClassroomDto>>;
