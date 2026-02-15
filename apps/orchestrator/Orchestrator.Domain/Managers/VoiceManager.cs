using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Orchestrates voice selection and preview operations.
/// </summary>
internal sealed class VoiceManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private AgentManager? _agentManager;
    private AgentManager AgentManager => _agentManager ??= new AgentManager(_serviceLocator);

    public VoiceManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Returns available voices (prebuilt from ElevenLabs; fake mode returns deterministic list).
    /// </summary>
    public async Task<IReadOnlyList<ElevenLabsVoiceItem>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
    {
        var list = await GatewayFacade.ListVoicesAsync(cancellationToken).ConfigureAwait(false);
        return list ?? (IReadOnlyList<ElevenLabsVoiceItem>)new List<ElevenLabsVoiceItem>();
    }

    /// <summary>
    /// Returns stock voices for "Choose a voice" - fetches directly from ElevenLabs API.
    /// </summary>
    public async Task<IReadOnlyList<StockVoice>> GetStockVoicesAsync(CancellationToken cancellationToken = default)
    {
        // Fetch voices from ElevenLabs API and convert to StockVoice format
        var elevenLabsVoices = await GatewayFacade.ListVoicesAsync(cancellationToken).ConfigureAwait(false);
        
        if (elevenLabsVoices == null || elevenLabsVoices.Count == 0)
        {
            return Array.Empty<StockVoice>();
        }
        
        // Convert ElevenLabsVoiceItem to StockVoice
        var stockVoices = new List<StockVoice>();
        var sortOrder = 0;
        foreach (var voice in elevenLabsVoices.Where(v => v.VoiceType == "prebuilt"))
        {
            stockVoices.Add(new StockVoice
            {
                Id = Guid.NewGuid(),
                VoiceId = voice.Id,
                Name = voice.Name,
                Description = voice.Category,
                Tags = voice.Category,
                PreviewText = voice.PreviewText ?? "Hello! I'm an AI interviewer.",
                SortOrder = sortOrder++
            });
        }
        
        return stockVoices;
    }

    /// <summary>
    /// Sets the agent's voice to the given prebuilt voice.
    /// </summary>
    public async Task SelectAgentVoiceAsync(Guid agentId, string voiceProvider, string voiceType, string voiceId, string? voiceName, CancellationToken cancellationToken = default)
    {
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found.");
        }

        agent.ElevenlabsVoiceId = voiceId;
        agent.VoiceProvider = voiceProvider;
        agent.VoiceType = voiceType;
        agent.VoiceName = voiceName ?? voiceId;
        await DataFacade.UpdateAgent(agent).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews a voice by generating a short sample (uses existing TTS path).
    /// </summary>
    public async Task<byte[]> PreviewVoiceAsync(string voiceId, string text, CancellationToken cancellationToken = default)
    {
        var config = GatewayFacade.GetElevenLabsConfig();
        if (!config.Enabled && !config.UseFakeElevenLabs)
        {
            throw new ElevenLabsDisabledException();
        }

        return await GatewayFacade.GenerateSpeechAsync(text, voiceId, 0.5m, 0.75m, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Previews the agent's current voice by generating a short TTS sample (uses existing TTS path).
    /// </summary>
    public async Task<byte[]> PreviewAgentVoiceAsync(Guid agentId, string text, CancellationToken cancellationToken = default)
    {
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found.");
        }

        var config = GatewayFacade.GetElevenLabsConfig();
        var voiceId = agent.ElevenlabsVoiceId ?? config.DefaultVoiceId;
        return await PreviewVoiceAsync(voiceId, text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams voice audio chunks as they are generated for low-latency playback.
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamVoiceAsync(string voiceId, string text, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = GatewayFacade.GetElevenLabsConfig();
        if (!config.Enabled && !config.UseFakeElevenLabs)
        {
            throw new ElevenLabsDisabledException();
        }

        await foreach (var chunk in GatewayFacade.StreamSpeechAsync(text, voiceId, 0.5m, 0.75m, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Warms up audio cache for interview questions by pre-generating TTS for all question texts.
    /// </summary>
    public async Task<InterviewAudioWarmupResult> WarmupInterviewAudioAsync(Guid interviewId, CancellationToken cancellationToken = default)
    {
        var result = new InterviewAudioWarmupResult { InterviewId = interviewId };

        // Get the interview
        var interview = await DataFacade.GetInterviewById(interviewId).ConfigureAwait(false);
        if (interview == null)
        {
            throw new InterviewNotFoundException($"Interview with ID {interviewId} not found");
        }

        // Get the agent for voice settings
        var agent = await AgentManager.GetAgentById(interview.AgentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {interview.AgentId} not found");
        }

        var config = GatewayFacade.GetElevenLabsConfig();
        var voiceId = agent.ElevenlabsVoiceId ?? config.DefaultVoiceId;
        
        // Load questions from interview configuration
        if (!interview.InterviewConfigurationId.HasValue)
        {
            return result; // No configuration, no questions to warm up
        }

        var configQuestions = await DataFacade.GetInterviewConfigurationQuestions(interview.InterviewConfigurationId.Value).ConfigureAwait(false);
        result.TotalQuestions = configQuestions.Count;

        // Pre-generate audio for each question
        foreach (var question in configQuestions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            try
            {
                // Generate audio (the cache manager handles checking/storing cache)
                await GatewayFacade.GenerateSpeechAsync(
                    question.Question,
                    voiceId,
                    0.5m,
                    0.75m,
                    cancellationToken
                ).ConfigureAwait(false);
                
                result.CachedQuestions++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to warm up audio for question: {ex.Message}");
                result.FailedQuestions++;
            }
        }

        return result;
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
        _agentManager?.Dispose();
    }
}
