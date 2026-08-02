namespace Draya.Domain.Classrooms.Exceptions;

public class ClassroomNotFoundException : Exception
{
    public ClassroomNotFoundException() 
        : base("Classroom not found.")
    {
    }
}
