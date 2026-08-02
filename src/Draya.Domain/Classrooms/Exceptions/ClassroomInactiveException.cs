namespace Draya.Domain.Classrooms.Exceptions;

public class ClassroomInactiveException : Exception
{
    public ClassroomInactiveException() 
        : base("This classroom is no longer active.")
    {
    }
}
