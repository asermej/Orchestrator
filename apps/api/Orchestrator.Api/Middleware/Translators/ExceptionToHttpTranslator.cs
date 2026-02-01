using Orchestrator.Domain;
using Microsoft.AspNetCore.Http.Features;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Middleware.Translators;

public static class ExceptionToHttpTranslator
{
    public static async Task Translate(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);
        
        // Get logger from DI
        var loggerFactory = httpContext.RequestServices.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ExceptionToHttpTranslator");
        
        // Log the exception details
        logger?.LogError(exception, 
            "Exception caught in middleware. Type: {ExceptionType}, Message: {Message}, Path: {Path}", 
            exception.GetType().Name, 
            exception.Message,
            httpContext.Request.Path);
        
        var httpResponse = httpContext.Response;
        httpResponse.Headers["Exception-Type"] = exception.GetType().Name;

        if (exception is BaseException baseException)
        {
            httpContext.Features.Get<IHttpResponseFeature>()!.ReasonPhrase = baseException.Reason;
            logger?.LogError("BaseException reason: {Reason}", baseException.Reason);
        }

        var statusCode = MapExceptionToStatusCode(exception);

        // Determine the user-facing message based on exception type
        string userFacingMessage;
        if (exception is ElevenLabsDisabledException disabledEx)
        {
            userFacingMessage = disabledEx.Message;
        }
        else if (exception is TechnicalBaseException)
        {
            // For technical exceptions, never expose internal details
            // Use a generic, user-friendly message
            userFacingMessage = "An error occurred. Please try again or contact support if the problem persists.";
        }
        else if (exception is BusinessBaseException businessEx)
        {
            // For business exceptions, use the Reason property which is user-friendly
            // Examples: "Topic not found", "User not found", "Invalid email format"
            // Reason does NOT contain internal IDs or technical details
            userFacingMessage = businessEx.Reason;
        }
        else if (exception is BaseException baseEx)
        {
            // For other BaseExceptions, use the Reason property
            userFacingMessage = baseEx.Reason;
        }
        else
        {
            // For any unhandled exception type, use a generic message
            userFacingMessage = "An unexpected error occurred. Please contact support if the problem persists.";
        }
        
        // Create structured error response
        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = userFacingMessage, // User-friendly message without internal IDs
            ExceptionType = exception.GetType().Name,
            IsBusinessException = exception is BusinessBaseException,
            IsTechnicalException = exception is TechnicalBaseException,
            Timestamp = DateTime.UtcNow
        };
        
        httpResponse.StatusCode = statusCode;
        httpResponse.ContentType = "application/json";
        
        logger?.LogError("Returning status code {StatusCode} with structured error response", statusCode);
        
        await httpResponse.WriteAsJsonAsync(errorResponse);
        await httpResponse.Body.FlushAsync();
    }

    private static int MapExceptionToStatusCode(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is VoiceCloneRateLimitExceededException)
        {
            return 429;
        }
        if (exception is ElevenLabsDisabledException)
        {
            return 503;
        }
        if (exception is NotFoundBaseException)
        {
            return 404;
        }
        if (exception is BusinessBaseException)
        {
            return 400;
        }

        return 500;
    }
}