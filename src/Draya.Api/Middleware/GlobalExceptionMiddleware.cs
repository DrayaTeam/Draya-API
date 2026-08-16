using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Identity.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Draya.Api.Middleware;

/// <summary>
/// Catches domain and application exceptions and maps them to the standard ErrorResponse
/// envelope defined in API_CONTRACT.md §1.4 — no raw exceptions ever reach the client.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, code, message, details) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "VALIDATION_FAILED",
                "One or more fields are invalid.",
                ve.Errors.Select(e => new { field = e.PropertyName, issue = e.ErrorMessage }).Cast<object>().ToList()
            ),
            DuplicateEmailException => (
                HttpStatusCode.Conflict,
                "EMAIL_ALREADY_EXISTS",
                exception.Message,
                (List<object>)[]
            ),
            InvalidCredentialsException => (
                HttpStatusCode.Unauthorized,
                "INVALID_CREDENTIALS",
                exception.Message,
                (List<object>)[]
            ),
            InvalidCurrentPasswordException => (
                HttpStatusCode.BadRequest,
                "INVALID_CURRENT_PASSWORD",
                exception.Message,
                (List<object>)[]
            ),
            AccountDeactivatedException => (
                HttpStatusCode.Forbidden,
                "ACCOUNT_DEACTIVATED",
                exception.Message,
                (List<object>)[]
            ),
            AccountLockedException => (
                HttpStatusCode.Locked,
                "ACCOUNT_LOCKED",
                exception.Message,
                (List<object>)[]
            ),
            InvalidRefreshTokenException => (
                HttpStatusCode.Unauthorized,
                "INVALID_REFRESH_TOKEN",
                exception.Message,
                (List<object>)[]
            ),
            InvalidPasswordResetTokenException => (
                HttpStatusCode.Unauthorized,
                "INVALID_PASSWORD_RESET_TOKEN",
                exception.Message,
                (List<object>)[]
            ),
            SubjectNotFoundException => (
                HttpStatusCode.NotFound,
                "SUBJECT_NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            ClassroomNotFoundException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            EnrollmentCodeInvalidException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            ClassroomInactiveException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            AlreadyEnrolledException => (
                HttpStatusCode.Conflict,
                "ALREADY_ENROLLED",
                exception.Message,
                (List<object>)[]
            ),
            StudentNotEnrolledException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            PaidClassroomRequiresCheckoutException => (
                HttpStatusCode.UnprocessableEntity,
                "PAID_CLASSROOM_REQUIRES_CHECKOUT",
                exception.Message,
                (List<object>)[]
            ),
            Draya.Domain.Wallets.Exceptions.InsufficientBalanceException => (
                HttpStatusCode.UnprocessableEntity,
                "INSUFFICIENT_BALANCE",
                exception.Message,
                (List<object>)[]
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                exception.Message,
                (List<object>)[]
            ),
            NotFoundException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message,
                (List<object>)[]
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred.",
                (List<object>)[]
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = new
            {
                code,
                message,
                details
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
