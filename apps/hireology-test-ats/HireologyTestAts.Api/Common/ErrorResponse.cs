namespace HireologyTestAts.Api.Common;

/// <summary>
/// Represents a structured error response from the API
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The error message from the exception
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The type of exception that occurred
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a business exception (user-facing validation or business rule violation)
    /// </summary>
    public bool IsBusinessException { get; set; }

    /// <summary>
    /// Indicates if this is a technical exception (system error, database error, etc.)
    /// </summary>
    public bool IsTechnicalException { get; set; }

    /// <summary>
    /// The timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
}
