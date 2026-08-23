using System;
using System.Collections.Generic;
using Draya.Application.Exams.DTOs;
using MediatR;

namespace Draya.Application.Exams.Queries.GetPendingReviews;

public record GetPendingReviewsQuery(Guid TeacherId) : IRequest<List<PendingReviewClassroomDto>>;
