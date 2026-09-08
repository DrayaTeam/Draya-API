namespace Draya.Domain.Classrooms.Exceptions;

public class ClassroomFeedbackAlreadySubmittedException : Exception
{
    public ClassroomFeedbackAlreadySubmittedException()
        : base("You have already submitted feedback for this classroom.")
    {
    }
}
