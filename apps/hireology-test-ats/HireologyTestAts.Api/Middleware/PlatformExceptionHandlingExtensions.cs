using Microsoft.AspNetCore.Builder;

namespace HireologyTestAts.Api.Middleware;

public static class PlatformExceptionHandlingExtensions
{
    public static IApplicationBuilder UsePlatformExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PlatformExceptionHandlingMiddleware>();
    }
}
