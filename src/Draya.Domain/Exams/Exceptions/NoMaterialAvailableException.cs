namespace Draya.Domain.Exams.Exceptions;

/// <summary>
/// Thrown when a teacher attempts to generate an exam for a section
/// that has no parsed course material to ground questions on.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class NoMaterialAvailableException : Exception
{
    public NoMaterialAvailableException(string message) : base(message) { }
}
