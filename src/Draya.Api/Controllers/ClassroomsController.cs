using Draya.Application.Classrooms.Commands.CreateClassroom;
using Draya.Application.Classrooms.Commands.CreateSubject;
using Draya.Application.Classrooms.Commands.DeactivateClassroom;
using Draya.Application.Classrooms.Commands.EnrollStudent;
using Draya.Application.Classrooms.Commands.RegenerateEnrollmentCode;
using Draya.Application.Classrooms.Commands.RemoveStudentFromClassroom;
using Draya.Application.Classrooms.Commands.UpdateClassroom;
using Draya.Application.Classrooms.Commands.UploadClassroomImage;
using Draya.Application.Classrooms.DTOs;
using Draya.Application.Classrooms.Feedback.Commands;
using Draya.Application.Classrooms.Feedback.DTOs;
using Draya.Application.Classrooms.Feedback.Queries;
using Draya.Application.Classrooms.Queries.GetClassroomDetails;
using Draya.Application.Classrooms.Queries.GetClassroomRoster;
using Draya.Application.Classrooms.Queries.GetSubjects;
using Draya.Application.Classrooms.Queries.GetTeacherClassrooms;
using Draya.Application.Classrooms.Queries.GetStudentClassrooms;
using Draya.Application.Classrooms.Queries.GetAllClassrooms;
using Draya.Application.Classrooms.Queries.GetClassroomTypes;
using Draya.Application.Classrooms.Queries.GetGradeLevels;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public class ClassroomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassroomsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("subjects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubjectDto>>> GetSubjects(CancellationToken cancellationToken)
    {
        var query = new GetSubjectsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("subjects")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectDto>> CreateSubject(
        [FromBody] CreateSubjectRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new CreateSubjectCommand(request.Name);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("classrooms")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClassroomDto>> CreateClassroom(
        [FromBody] CreateClassroomRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetUserId();
        var command = new CreateClassroomCommand(
            teacherId, 
            request.SubjectId, 
            request.Name,
            request.ClassroomTypeId,
            request.GradeLevelId,
            request.StartDate,
            request.EndDate,
            request.Price,
            request.ImageUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("classrooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassroomDto>>> GetClassrooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? gradeLevelId = null,
        [FromQuery] Guid? classroomTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole == "Teacher")
        {
            // Note: Currently GetTeacherClassroomsQuery doesn't support filters, but we could add them if needed.
            var query = new GetTeacherClassroomsQuery(userId, page, pageSize);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        else if (userRole == "Student")
        {
            var query = new GetStudentClassroomsQuery(userId, page, pageSize, subjectId, gradeLevelId, classroomTypeId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        return BadRequest(new { error = new { message = "Unsupported role for classroom listing." } });
    }

    [HttpGet("classrooms/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassroomDto>>> SearchClassrooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? gradeLevelId = null,
        [FromQuery] Guid? classroomTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllClassroomsQuery(page, pageSize, subjectId, gradeLevelId, classroomTypeId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("classrooms/types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClassroomTypeDto>>> GetClassroomTypes(CancellationToken cancellationToken)
    {
        var query = new GetClassroomTypesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("classrooms/grade-levels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GradeLevelDto>>> GetGradeLevels(CancellationToken cancellationToken)
    {
        var query = new GetGradeLevelsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("classrooms/{classroomId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomDto>> GetClassroomDetails(
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        var query = new GetClassroomDetailsQuery(classroomId, userId, userRole);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("classrooms/{classroomId}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomDto>> UpdateClassroom(
        Guid classroomId,
        [FromBody] UpdateClassroomRequest request,
        CancellationToken cancellationToken)
    {
        var teacherId = GetUserId();
        var command = new UpdateClassroomCommand(
            classroomId,
            teacherId,
            request.Name,
            request.SubjectId,
            request.ClassroomTypeId,
            request.GradeLevelId,
            request.StartDate,
            request.EndDate,
            request.Price,
            request.IsActive,
            request.ImageUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("classrooms/{classroomId}/feedback")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ClassroomFeedbackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassroomFeedbackDto>> SubmitClassroomFeedback(
        Guid classroomId,
        [FromBody] SubmitClassroomFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new SubmitClassroomFeedbackCommand(
                    classroomId,
                    GetUserId(),
                    request.Rating,
                    request.Comment),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ClassroomFeedbackAlreadySubmittedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ClassroomFeedbackRequiresEnrollmentException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("classrooms/{classroomId}/feedback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassroomFeedbackSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomFeedbackSummaryDto>> GetClassroomFeedback(
        Guid classroomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetClassroomFeedbackSummaryQuery(
                classroomId,
                page,
                pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("classrooms/{classroomId}/image")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ClassroomImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomImageResponse>> UploadClassroomImage(
        Guid classroomId,
        [FromForm] UploadClassroomImageRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = new { message = "Image file is required." } });
        }

        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = new { message = "Image file cannot exceed 5MB." } });
        }

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png"
        };

        if (!allowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { error = new { message = "Only JPEG and PNG images are supported." } });
        }

        await using var stream = file.OpenReadStream();
        var imageUrl = await _mediator.Send(
            new UploadClassroomImageCommand(
                classroomId,
                GetUserId(),
                stream,
                file.FileName,
                file.ContentType),
            cancellationToken);

        return Ok(new ClassroomImageResponse(imageUrl));
    }

    [HttpDelete("classrooms/{classroomId}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateClassroom(
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetUserId();
        var command = new DeactivateClassroomCommand(classroomId, teacherId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("classrooms/{classroomId}/regenerate-code")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomDto>> RegenerateEnrollmentCode(
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetUserId();
        var command = new RegenerateEnrollmentCodeCommand(classroomId, teacherId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("classrooms/enroll")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EnrollmentResultDto>> EnrollStudent(
        [FromBody] EnrollStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = GetUserId();
            var command = new EnrollStudentCommand(studentId, request.EnrollmentCode);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (AlreadyEnrolledException)
        {
            return Conflict(new { message = "Student is already enrolled in this classroom." });
        }
    }

    [HttpPost("classrooms/{classroomId}/checkout")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutResponse>> CheckoutClassroom(
        Guid classroomId,
        [FromBody] CheckoutClassroomRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = GetUserId();
        var command = new Draya.Application.Classrooms.Commands.CheckoutClassroom.CheckoutClassroomCommand(studentId, classroomId, request.RedirectionUrl);
        var url = await _mediator.Send(command, cancellationToken);
        return Ok(new CheckoutResponse(url));
    }

    [HttpGet("classrooms/{classroomId}/students")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StudentRosterItemDto>>> GetClassroomRoster(
        Guid classroomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var teacherId = GetUserId();
        var query = new GetClassroomRosterQuery(classroomId, teacherId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("classrooms/{classroomId}/students/{studentId}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudentFromClassroom(
        Guid classroomId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetUserId();
        var command = new RemoveStudentFromClassroomCommand(classroomId, studentId, teacherId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}

public record CreateSubjectRequest(string Name);
public record CreateClassroomRequest(Guid SubjectId, string Name, Guid ClassroomTypeId, Guid GradeLevelId, DateTime StartDate, DateTime EndDate, decimal Price, string? ImageUrl = null);
public record UpdateClassroomRequest(string Name, Guid SubjectId, Guid ClassroomTypeId, Guid GradeLevelId, DateTime StartDate, DateTime EndDate, decimal Price, bool IsActive, string? ImageUrl = null);
public record SubmitClassroomFeedbackRequest(int Rating, string? Comment = null);
public record UploadClassroomImageRequest(IFormFile File);
public record ClassroomImageResponse(string ImageUrl);
public record EnrollStudentRequest(string EnrollmentCode);
public record CheckoutResponse(string CheckoutUrl);
public record CheckoutClassroomRequest(string RedirectionUrl);
