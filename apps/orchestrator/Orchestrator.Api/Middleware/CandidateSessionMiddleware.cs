using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Orchestrator.Domain;

namespace Orchestrator.Api.Middleware;

/// <summary>
/// Middleware for candidate session JWT authentication on /api/v1/candidate/* endpoints.
/// Validates the JWT, checks the jti against the candidate_sessions table, and
/// stores InterviewId, InviteId, OrgId in HttpContext.Items.
/// </summary>
public class CandidateSessionMiddleware
{
    private readonly RequestDelegate _next;
    private const string CandidateCookieName = "candidate_session";
    private const string CandidatePathPrefix = "/api/v1/candidate";
    private const string SessionsEndpoint = "/api/v1/candidate/sessions";

    public CandidateSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DomainFacade domainFacade)
    {
        // Only apply to /api/v1/candidate/* endpoints
        if (!context.Request.Path.StartsWithSegments(CandidatePathPrefix))
        {
            await _next(context);
            return;
        }

        // Skip auth for the sessions endpoint (it's AllowAnonymous)
        if (context.Request.Path.StartsWithSegments(SessionsEndpoint))
        {
            await _next(context);
            return;
        }

        // Extract JWT from cookie or Authorization header
        string? token = null;

        if (context.Request.Cookies.TryGetValue(CandidateCookieName, out var cookieToken))
        {
            token = cookieToken;
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = headerValue["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Candidate session token is required" });
            return;
        }

        try
        {
            // Get the signing secret
            var secret = domainFacade.GetCandidateTokenSecret();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            // Validate JWT signature and claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "hireology-candidate",
                ValidateAudience = true,
                ValidAudience = "hireology-api",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract claims
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var interviewIdClaim = principal.FindFirst("interview_id")?.Value;
            var inviteIdClaim = principal.FindFirst("invite_id")?.Value;
            var groupIdClaim = principal.FindFirst("group_id")?.Value;

            if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(interviewIdClaim))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid candidate session token: missing required claims" });
                return;
            }

            // Validate jti against the database (session must exist and be active)
            var session = await domainFacade.ValidateCandidateSession(jti);

            // Store context for controllers
            context.Items["InterviewId"] = session.InterviewId;
            context.Items["InviteId"] = session.InviteId;
            context.Items["CandidateSessionId"] = session.Id;

            if (Guid.TryParse(groupIdClaim, out var groupId))
            {
                context.Items["GroupId"] = groupId;
            }

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Candidate session token has expired" });
        }
        catch (SecurityTokenException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid candidate session token" });
        }
        catch (CandidateSessionNotFoundException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Candidate session not found or revoked" });
        }
        catch (CandidateSessionExpiredException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Candidate session has expired" });
        }
    }
}

/// <summary>
/// Extension methods for candidate session middleware
/// </summary>
public static class CandidateSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseCandidateSessionAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CandidateSessionMiddleware>();
    }
}
