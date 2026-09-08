using System;
using Draya.Application.Admin.DTOs;
using Draya.Application.Common.Models;
using MediatR;

namespace Draya.Application.Admin.Queries.GetManualAdjustments;

public record GetManualAdjustmentsQuery(
    Guid? TeacherId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedResult<ManualAdjustmentDto>>;
