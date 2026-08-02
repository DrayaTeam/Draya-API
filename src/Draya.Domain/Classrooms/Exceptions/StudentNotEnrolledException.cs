namespace Draya.Domain.Classrooms.Exceptions;

public class StudentNotEnrolledException : Exception
{
    public StudentNotEnrolledException() 
        : base("Student is not enrolled in this classroom.")
    {
    }
}
