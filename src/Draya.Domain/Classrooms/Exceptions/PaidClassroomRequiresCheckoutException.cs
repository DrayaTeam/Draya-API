namespace Draya.Domain.Classrooms.Exceptions;

public class PaidClassroomRequiresCheckoutException : Exception
{
    public PaidClassroomRequiresCheckoutException() 
        : base("This classroom requires payment. Please use the enrollment-checkout flow.")
    {
    }
}
