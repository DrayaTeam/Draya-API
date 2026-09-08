using Draya.Application.Payments.Services;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Payments;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CheckoutClassroom;

public class CheckoutClassroomCommandHandler : IRequestHandler<CheckoutClassroomCommand, string>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly IPaymobService _paymobService;

    public CheckoutClassroomCommandHandler(
        IClassroomRepository classroomRepository,
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IPaymentTransactionRepository paymentTransactionRepository,
        IPaymobService paymobService)
    {
        _classroomRepository = classroomRepository;
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _paymobService = paymobService;
    }

    public async Task<string> Handle(CheckoutClassroomCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RedirectionUrl))
            throw new ArgumentException("Redirection URL is required.");

        if (!Uri.TryCreate(request.RedirectionUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid Redirection URL format.");

        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);

        if (classroom == null || !classroom.IsActive)
        {
            throw new ClassroomNotFoundException();
        }

        var isEnrolled = await _enrollmentRepository.GetByStudentAndClassroomAsync(request.StudentId, request.ClassroomId, cancellationToken);

        if (isEnrolled != null && isEnrolled.Status == EnrollmentStatus.Active)
        {
            throw new AlreadyEnrolledException();
        }

        var student = await _studentRepository.GetByUserIdAsync(request.StudentId, cancellationToken);

        if (student == null)
        {
            throw new UnauthorizedAccessException("Student not found.");
        }

        var paymentTransaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Purpose = PaymentPurpose.ClassroomEnrollment,
            PayerId = request.StudentId,
            ClassroomId = request.ClassroomId,
            GrossAmount = classroom.Price,
            Status = PaymentStatus.Pending,
            RedirectionUrl = request.RedirectionUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentTransactionRepository.AddAsync(paymentTransaction, cancellationToken);
        await _paymentTransactionRepository.SaveChangesAsync(cancellationToken);

        string email = !string.IsNullOrEmpty(student.ParentGuardianEmail) ? student.ParentGuardianEmail : "no-email@draya.com";
        string fullName = student.FullName ?? "Student";
        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
        string lastName = nameParts.Length > 1 ? nameParts[1] : "Unknown";
        string phone = "01000000000"; // Default phone as it's not present on Student

        var checkoutUrl = await _paymobService.CreateCheckoutUrlAsync(
            paymentTransaction.Id, 
            paymentTransaction.GrossAmount, 
            email, 
            firstName, 
            lastName, 
            phone,
            request.RedirectionUrl,
            cancellationToken);

        return checkoutUrl;
    }
}
