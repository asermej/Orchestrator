using Orchestrator.Api.Middleware.Translators;

namespace Orchestrator.Api.Middleware;

public sealed class PlatformExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PlatformExceptionHandlingMiddleware> _logger;

    public PlatformExceptionHandlingMiddleware(RequestDelegate next, ILogger<PlatformExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught for {Method} {Path}: {Message}", 
                context.Request.Method, 
                context.Request.Path, 
                ex.Message);
            
            await ExceptionToHttpTranslator.Translate(context, ex);
        }
    }
}
