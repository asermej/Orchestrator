using HireologyTestAts.Domain;
using Microsoft.AspNetCore.Http.Features;
using HireologyTestAts.Api.Common;

namespace HireologyTestAts.Api.Middleware.Translators;

public static class ExceptionToHttpTranslator
{
    public static async Task Translate(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var loggerFactory = httpContext.RequestServices.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ExceptionToHttpTranslator");

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

        string userFacingMessage;
        if (exception is TechnicalBaseException)
        {
            userFacingMessage = "An error occurred. Please try again or contact support if the problem persists.";
        }
        else if (exception is BusinessBaseException businessEx)
        {
            userFacingMessage = businessEx.Reason;
        }
        else if (exception is BaseException baseEx)
        {
            userFacingMessage = baseEx.Reason;
        }
        else
        {
            userFacingMessage = "An unexpected error occurred. Please contact support if the problem persists.";
        }

        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = userFacingMessage,
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
