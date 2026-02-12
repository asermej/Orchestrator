using System.Security.Claims;
using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UsersRepository _users;
    private readonly OrchestratorUserProvisioningService? _orchestratorProvisioning;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UsersRepository users,
        OrchestratorUserProvisioningService? orchestratorProvisioning = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _users = users;
        _orchestratorProvisioning = orchestratorProvisioning;
    }

    public string? GetAuth0Sub()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
    }

    public async Task<UserItem?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var sub = GetAuth0Sub();
        if (string.IsNullOrEmpty(sub)) return null;

        var user = await _users.GetByAuth0SubAsync(sub, ct);
        if (user != null) return user;

        // First login: create user from JWT claims
        var email = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");
        var name = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name");

        user = new UserItem
        {
            Auth0Sub = sub,
            Email = email,
            Name = name
        };
        user = await _users.CreateAsync(user, ct);

        if (_orchestratorProvisioning != null)
        {
            try
            {
                await _orchestratorProvisioning.ProvisionUserAsync(sub, email, name, ct);
            }
            catch
            {
                // Non-fatal: test-ats user is created; Orchestrator provisioning can be retried later
            }
        }

        return user;
    }
}
