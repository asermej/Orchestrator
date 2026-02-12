using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages streaming voice conversation operations.
/// Orchestrates OpenAI text generation and ElevenLabs TTS with per-sentence buffering.
/// </summary>
internal sealed class ConversationManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private AgentManager? _agentManager;
    private AgentManager AgentManager => _agentManager ??= new AgentManager(_serviceLocator);

    // Abbreviations to skip when detecting sentence boundaries
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dr.", "Mr.", "Mrs.", "Ms.", "Prof.", "Jr.", "Sr.", "vs.", "etc.", "Inc.", "Ltd.", "Corp.",
        "Ave.", "Blvd.", "St.", "Rd.", "Mt.", "ft.", "lb.", "oz.", "pt.", "qt.", "gal."
    };

    public ConversationManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Streams audio response for a voice conversation (stateless; no persistence).
    /// Generates AI response and streams TTS audio.
    /// </summary>
    /// <param name="agentId">The agent ID for voice settings</param>
    /// <param name="userMessage">The user's text message (transcribed from speech)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of audio chunks (MP3 bytes)</returns>
    public async IAsyncEnumerable<byte[]> StreamAudioResponseAsync(
        Guid agentId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get ElevenLabs config for limits
        var config = GatewayFacade.GetElevenLabsConfig();
        
        if (!config.Enabled)
        {
            throw new ElevenLabsDisabledException("ElevenLabs TTS is disabled in configuration");
        }

        // Get agent for voice settings
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found");
        }

        // Build minimal in-request history (single user turn) for AI context
        var chatHistory = new List<ConversationTurn>
        {
            new ConversationTurn { Role = "user", Content = userMessage }
        };

        // Generate AI response text
        var aiResponseText = await GenerateAIResponse(agentId, chatHistory).ConfigureAwait(false);

        // Stream TTS for each sentence
        var voiceId = agent.ElevenlabsVoiceId ?? config.DefaultVoiceId;
        var stability = agent.VoiceStability;
        var similarityBoost = agent.VoiceSimilarityBoost;
        var requestCount = 0;

        await foreach (var sentence in BufferSentences(aiResponseText, config.MaxCharsPerRequest).ConfigureAwait(false))
        {
            if (requestCount >= config.MaxRequestsPerMessage)
            {
                // Limit reached, stop streaming audio
                break;
            }

            requestCount++;

            // Stream audio for this sentence
            await foreach (var chunk in GatewayFacade.StreamSpeechAsync(sentence, voiceId, stability, similarityBoost, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// Buffers text and yields complete sentences.
    /// Handles abbreviations and splits long sentences.
    /// </summary>
    private async IAsyncEnumerable<string> BufferSentences(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var buffer = new StringBuilder();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            buffer.Append(word);
            buffer.Append(' ');

            // Check if this word ends with a sentence terminator
            if (IsSentenceEnd(word, buffer.ToString()))
            {
                var sentence = buffer.ToString().Trim();
                
                // Handle sentences that exceed max chars
                if (sentence.Length > maxChars)
                {
                    foreach (var chunk in SplitLongSentence(sentence, maxChars))
                    {
                        yield return chunk;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(sentence))
                {
                    yield return sentence;
                }
                
                buffer.Clear();
            }
        }

        // Flush remaining buffer
        if (buffer.Length > 0)
        {
            var remaining = buffer.ToString().Trim();
            if (remaining.Length > maxChars)
            {
                foreach (var chunk in SplitLongSentence(remaining, maxChars))
                {
                    yield return chunk;
                }
            }
            else if (!string.IsNullOrWhiteSpace(remaining))
            {
                yield return remaining;
            }
        }

        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <summary>
    /// Determines if a word represents the end of a sentence.
    /// </summary>
    private bool IsSentenceEnd(string word, string context)
    {
        // Check for sentence-ending punctuation
        if (!word.EndsWith('.') && !word.EndsWith('!') && !word.EndsWith('?'))
        {
            return false;
        }

        // Check if this is an abbreviation
        if (Abbreviations.Contains(word))
        {
            return false;
        }

        // Check for common patterns that aren't sentence ends
        // e.g., "U.S." or "a.m."
        if (Regex.IsMatch(word, @"^[A-Z]\.[A-Z]\.$", RegexOptions.IgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Splits a long sentence at commas or hard-wraps at maxChars.
    /// </summary>
    private IEnumerable<string> SplitLongSentence(string sentence, int maxChars)
    {
        // First try splitting on commas
        var parts = sentence.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            
            if (currentChunk.Length + trimmedPart.Length + 2 <= maxChars) // +2 for ", "
            {
                if (currentChunk.Length > 0)
                {
                    currentChunk.Append(", ");
                }
                currentChunk.Append(trimmedPart);
            }
            else
            {
                if (currentChunk.Length > 0)
                {
                    yield return currentChunk.ToString();
                    currentChunk.Clear();
                }

                // If the part itself is too long, hard-wrap it
                if (trimmedPart.Length > maxChars)
                {
                    foreach (var chunk in HardWrap(trimmedPart, maxChars))
                    {
                        yield return chunk;
                    }
                }
                else
                {
                    currentChunk.Append(trimmedPart);
                }
            }
        }

        if (currentChunk.Length > 0)
        {
            yield return currentChunk.ToString();
        }
    }

    /// <summary>
    /// Hard-wraps text at maxChars boundaries.
    /// </summary>
    private IEnumerable<string> HardWrap(string text, int maxChars)
    {
        for (var i = 0; i < text.Length; i += maxChars)
        {
            var length = Math.Min(maxChars, text.Length - i);
            yield return text.Substring(i, length);
        }
    }

    /// <summary>
    /// Generates AI response using OpenAI.
    /// </summary>
    private async Task<string> GenerateAIResponse(Guid agentId, IEnumerable<ConversationTurn> chatHistory)
    {
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found");
        }

        var generalTraining = await AgentManager.GetAgentTraining(agentId).ConfigureAwait(false);
        
        var topicContexts = new List<(string TopicName, string Content)>();

        var systemPrompt = BuildSystemPrompt(agent, generalTraining, topicContexts);
        return await GatewayFacade.GenerateChatCompletion(systemPrompt, chatHistory).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds system prompt for the agent.
    /// </summary>
    private string BuildSystemPrompt(Agent agent, string? generalTraining, List<(string TopicName, string Content)> topicContexts)
    {
        var promptParts = new List<string>
        {
            $"You are {agent.DisplayName}."
        };

        if (!string.IsNullOrWhiteSpace(generalTraining))
        {
            promptParts.Add("\n\n## Background and Training");
            promptParts.Add(generalTraining);
        }

        if (topicContexts.Any())
        {
            promptParts.Add("\n\n## Conversation Topic Context");
            promptParts.Add("The following topics have been loaded into this conversation:");
            foreach (var (topicName, content) in topicContexts)
            {
                promptParts.Add($"\n### {topicName}");
                promptParts.Add(content);
            }
        }

        promptParts.Add("\n\n## Response Guidelines");
        promptParts.Add("Respond in character, maintaining your personality throughout the conversation.");
        promptParts.Add("Keep responses conversational and natural for voice interaction.");
        promptParts.Add("Avoid using markdown formatting, bullet points, or other text-only formatting.");

        return string.Join(" ", promptParts);
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
        _agentManager?.Dispose();
    }
}
