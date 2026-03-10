using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using Orchestrator.Api.Common;
using Orchestrator.Domain;

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

        var (statusCode, userFacingMessage) = MapExceptionToStatusCodeAndMessage(exception);

        // Determine the user-facing message based on exception type (override if not set by mapper)
        if (userFacingMessage == null)
        {
            userFacingMessage = GetUserFacingMessage(exception);
        }
        else
        {
            // statusCode already set by mapper
        }

        // 409 Conflict with our message is user-facing (e.g. duplicate question label)
        var isUserFacing = exception is BusinessBaseException || statusCode == 409;
        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = userFacingMessage, // User-friendly message without internal IDs
            ExceptionType = exception.GetType().Name,
            IsBusinessException = isUserFacing,
            IsTechnicalException = exception is TechnicalBaseException,
            Timestamp = DateTime.UtcNow
        };

        httpResponse.StatusCode = statusCode;
        httpResponse.ContentType = "application/json";

        logger?.LogError("Returning status code {StatusCode} with structured error response", statusCode);

        await httpResponse.WriteAsJsonAsync(errorResponse);
        await httpResponse.Body.FlushAsync();
    }

    private static string GetUserFacingMessage(Exception exception)
    {
        if (exception is ElevenLabsDisabledException disabledEx)
            return disabledEx.Message;
        if (exception is TechnicalBaseException)
            return "An error occurred. Please try again or contact support if the problem persists.";
        if (exception is BusinessBaseException businessEx)
            return businessEx.Reason;
        if (exception is BaseException baseEx)
            return baseEx.Reason;
        return "An unexpected error occurred. Please contact support if the problem persists.";
    }

    private static (int StatusCode, string? UserFacingMessage) MapExceptionToStatusCodeAndMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is ElevenLabsDisabledException)
            return (503, null);
        if (exception is NotFoundBaseException)
            return (404, null);
        if (exception is BusinessBaseException)
            return (400, null);

        return (500, null);
    }
}