namespace Draya.Domain.Classrooms.Exceptions;

public class SubjectNotFoundException : Exception
{
    public SubjectNotFoundException(Guid subjectId) 
        : base($"Subject with ID '{subjectId}' not found.")
    {
    }
}
