using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Auth;

/// <summary>
/// Action filter that requires the current user to be a group admin for the
/// group specified by the "groupId" route parameter, OR a superadmin.
/// Returns 403 if the user is neither.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class GroupAdminRequiredAttribute : Attribute, IAsyncActionFilter
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

        // Superadmins always pass
        var isSuperadmin = await domainFacade.IsSuperadminByAuth0Sub(auth0Sub);
        if (isSuperadmin)
        {
            await next();
            return;
        }

        // Extract groupId from route values
        if (!context.ActionArguments.TryGetValue("groupId", out var groupIdObj)
            && !context.RouteData.Values.TryGetValue("groupId", out groupIdObj))
        {
            context.Result = new BadRequestObjectResult(new
            {
                StatusCode = 400,
                Message = "Group ID is required",
                ExceptionType = "ValidationException",
                IsBusinessException = true,
                IsTechnicalException = false,
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        Guid groupId;
        if (groupIdObj is Guid gid)
        {
            groupId = gid;
        }
        else if (!Guid.TryParse(groupIdObj?.ToString(), out groupId))
        {
            context.Result = new BadRequestObjectResult(new
            {
                StatusCode = 400,
                Message = "Invalid Group ID",
                ExceptionType = "ValidationException",
                IsBusinessException = true,
                IsTechnicalException = false,
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        // Resolve the current user
        var email = context.HttpContext.User?.FindFirstValue(ClaimTypes.Email)
            ?? context.HttpContext.User?.FindFirstValue("email");
        var name = context.HttpContext.User?.FindFirstValue(ClaimTypes.Name)
            ?? context.HttpContext.User?.FindFirstValue("name");

        var user = await domainFacade.GetOrCreateUser(auth0Sub, email, name);
        var isGroupAdmin = await domainFacade.IsGroupAdmin(user.Id, groupId);

        if (!isGroupAdmin)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = 403,
                Message = "Group admin privileges required for this group",
                ExceptionType = "AccessDeniedException",
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
