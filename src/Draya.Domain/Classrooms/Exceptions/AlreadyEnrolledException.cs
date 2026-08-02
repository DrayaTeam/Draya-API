namespace Draya.Domain.Classrooms.Exceptions;

public class AlreadyEnrolledException : Exception
{
    public AlreadyEnrolledException() 
        : base("Student is already enrolled in this classroom.")
    {
    }
}
