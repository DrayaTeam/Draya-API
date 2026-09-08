using System;

namespace Draya.Infrastructure.AI.Router;

public enum AiErrorType
{
    None,
    // Key-Level Failures
    KeyRateLimited,        
    InvalidCredential,     

    // Route-Level Failures
    ProviderRateLimited,   
    QuotaExhausted,        

    // Retriable Failures
    TransientUnavailable,  
    UnknownProviderFailure,

    // Client Errors
    InvalidRequest         
}

public class AiProviderException : Exception
{
    public AiErrorType ErrorType { get; }
    public int? StatusCode { get; }
    public string? CorrelationId { get; }

    public AiProviderException(
        AiErrorType errorType,
        string message,
        int? statusCode = null,
        string? correlationId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorType = errorType;
        StatusCode = statusCode;
        CorrelationId = correlationId;
    }
}
