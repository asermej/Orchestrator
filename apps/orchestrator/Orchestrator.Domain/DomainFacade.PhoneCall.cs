using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// DomainFacade partial class for phone call operations
/// </summary>
public sealed partial class DomainFacade
{
    private PhoneCallManager? _phoneCallManager;
    private PhoneCallManager PhoneCallManager => _phoneCallManager ??= new PhoneCallManager(_serviceLocator);

    /// <summary>
    /// Creates a new phone call session for an incoming call, configured with the specified agent's
    /// persona, training, and voice settings.
    /// </summary>
    /// <param name="agentId">The agent whose persona will handle the call</param>
    /// <returns>A PhoneCallSession ready to be started. Caller is responsible for disposing.</returns>
    public async Task<PhoneCallSession> CreatePhoneCallSessionAsync(Guid agentId)
    {
        return await PhoneCallManager.CreateSessionAsync(agentId).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a phone call session with a raw system prompt (no agent lookup).
    /// </summary>
    public PhoneCallSession CreatePhoneCallSession(string systemPrompt, string? voiceId = null)
    {
        return PhoneCallManager.CreateSession(systemPrompt, voiceId);
    }
}
