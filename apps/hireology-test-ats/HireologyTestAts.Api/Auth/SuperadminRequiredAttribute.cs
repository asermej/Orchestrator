using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Auth;

/// <summary>
/// Action filter that requires the current user to be a superadmin.
/// Returns 403 if the user is not a superadmin.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SuperadminRequiredAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth0Sub = context.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.HttpContext.User?.FindFirstValue("sub");

        if (string.IsNullOrEmpty(auth0Sub))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var domainFacade = context.HttpContext.RequestServices.GetRequiredService<DomainFacade>();
        var isSuperadmin = await domainFacade.IsSuperadminByAuth0Sub(auth0Sub);

        if (!isSuperadmin)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = 403,
                Message = "Superadmin privileges required",
                ExceptionType = "SuperadminRequiredException",
                IsBusinessException = true,
                IsTechnicalException = false,
                Timestamp = DateTime.UtcNow
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
