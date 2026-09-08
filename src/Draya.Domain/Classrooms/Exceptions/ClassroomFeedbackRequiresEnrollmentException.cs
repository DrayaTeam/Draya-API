namespace Draya.Domain.Classrooms.Exceptions;

public class ClassroomFeedbackRequiresEnrollmentException : Exception
{
    public ClassroomFeedbackRequiresEnrollmentException()
        : base("You must be enrolled in this classroom to leave feedback.")
    {
    }
}
