namespace Draya.Application.Classrooms.DTOs;

public record EnrollmentResultDto(
    Guid ClassroomId,
    DateTime EnrolledAt,
    bool IsReEnrollment
);
