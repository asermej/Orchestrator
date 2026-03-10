using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Creates and manages PhoneCallSession instances for incoming phone calls.
/// </summary>
internal sealed class PhoneCallManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private AgentManager? _agentManager;
    private AgentManager AgentManager => _agentManager ??= new AgentManager(_serviceLocator);

    public PhoneCallManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Creates a new PhoneCallSession for an incoming call, configured with the given agent's
    /// system prompt and voice settings.
    /// </summary>
    /// <param name="agentId">The agent whose persona will handle the call</param>
    /// <returns>A configured PhoneCallSession ready to be started</returns>
    public async Task<PhoneCallSession> CreateSessionAsync(Guid agentId)
    {
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found");
        }

        var behavioralPrompt = AgentSystemPromptBuilder.Build(agent);
        var systemPrompt = $"You are {agent.DisplayName}.\n\n{behavioralPrompt}\n\n## Response Guidelines\nYou are on a phone call. Respond naturally and conversationally.\nKeep responses concise — one or two sentences at a time.\nDo not use markdown, bullet points, or any text formatting.\nSpeak as if talking to someone on the phone.";
        var voiceId = agent.ElevenlabsVoiceId;

        return new PhoneCallSession(GatewayFacade, systemPrompt, voiceId);
    }

    /// <summary>
    /// Creates a PhoneCallSession with a raw system prompt (no agent lookup).
    /// Useful for testing or simple configurations.
    /// </summary>
    public PhoneCallSession CreateSession(string systemPrompt, string? voiceId = null)
    {
        return new PhoneCallSession(GatewayFacade, systemPrompt, voiceId);
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
        _agentManager?.Dispose();
    }
}
