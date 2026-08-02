namespace Draya.Domain.Classrooms.Exceptions;

public class EnrollmentCodeInvalidException : Exception
{
    public EnrollmentCodeInvalidException() 
        : base("Invalid enrollment code.")
    {
    }
}
