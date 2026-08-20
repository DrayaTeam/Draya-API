namespace Draya.Application.Classrooms.DTOs;

public record StudentRosterItemDto(
    Guid StudentId,
    string FullName,
    DateTime EnrolledAt,
    string Status,
    string? ProfilePicture
);
