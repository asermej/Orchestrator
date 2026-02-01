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
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private PersonaManager? _personaManager;
    private PersonaManager PersonaManager => _personaManager ??= new PersonaManager(_serviceLocator);
    private MessageManager? _messageManager;
    private MessageManager MessageManager => _messageManager ??= new MessageManager(_serviceLocator);
    private ChatManager? _chatManager;
    private ChatManager ChatManager => _chatManager ??= new ChatManager(_serviceLocator);
    private TrainingStorageManager? _storageManager;
    private TrainingStorageManager StorageManager => _storageManager ??= new TrainingStorageManager();

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
    /// Streams audio response for a voice conversation.
    /// Saves user message, generates AI response, and streams TTS audio.
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="personaId">The persona ID for voice settings</param>
    /// <param name="userMessage">The user's text message (transcribed from speech)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of audio chunks (MP3 bytes)</returns>
    public async IAsyncEnumerable<byte[]> StreamAudioResponseAsync(
        Guid chatId,
        Guid personaId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get ElevenLabs config for limits
        var config = GatewayFacade.GetElevenLabsConfig();
        
        if (!config.Enabled)
        {
            throw new ElevenLabsDisabledException("ElevenLabs TTS is disabled in configuration");
        }

        // Save user message
        var message = new Message
        {
            ChatId = chatId,
            Role = "user",
            Content = userMessage
        };
        MessageValidator.Validate(message);
        await DataFacade.AddMessage(message).ConfigureAwait(false);

        // Get persona for voice settings
        var persona = await PersonaManager.GetPersonaById(personaId).ConfigureAwait(false);
        if (persona == null)
        {
            throw new PersonaNotFoundException($"Persona with ID {personaId} not found");
        }

        // Get chat history
        var chatHistory = await DataFacade.SearchMessages(chatId, null, null, 1, 50).ConfigureAwait(false);

        // Generate AI response text
        var aiResponseText = await GenerateAIResponse(personaId, chatId, chatHistory.Items).ConfigureAwait(false);

        // Save AI response
        var aiMessage = new Message
        {
            ChatId = chatId,
            Role = "assistant",
            Content = aiResponseText
        };
        MessageValidator.Validate(aiMessage);
        await DataFacade.AddMessage(aiMessage).ConfigureAwait(false);

        // Update chat's LastMessageAt
        var chat = await DataFacade.GetChatById(chatId).ConfigureAwait(false);
        if (chat != null)
        {
            chat.LastMessageAt = DateTime.UtcNow;
            await DataFacade.UpdateChat(chat).ConfigureAwait(false);
        }

        // Stream TTS for each sentence
        var voiceId = persona.ElevenLabsVoiceId ?? config.DefaultVoiceId;
        var stability = persona.VoiceStability;
        var similarityBoost = persona.VoiceSimilarityBoost;
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
    private async Task<string> GenerateAIResponse(Guid personaId, Guid chatId, IEnumerable<Message> chatHistory)
    {
        var persona = await PersonaManager.GetPersonaById(personaId).ConfigureAwait(false);
        if (persona == null)
        {
            throw new PersonaNotFoundException($"Persona with ID {personaId} not found");
        }

        var generalTraining = await PersonaManager.GetPersonaTraining(personaId).ConfigureAwait(false);
        var chatTopics = await ChatManager.GetChatTopics(chatId).ConfigureAwait(false);
        
        var topicContexts = new List<(string TopicName, string Content)>();
        foreach (var chatTopic in chatTopics)
        {
            var topic = await DataFacade.GetTopicById(chatTopic.TopicId).ConfigureAwait(false);
            if (topic != null && !string.IsNullOrWhiteSpace(topic.ContentUrl))
            {
                var topicContent = await StorageManager.GetTrainingFromUrl(topic.ContentUrl).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(topicContent))
                {
                    topicContexts.Add((topic.Name, topicContent));
                }
            }
        }

        var systemPrompt = BuildSystemPrompt(persona, generalTraining, topicContexts);
        return await GatewayFacade.GenerateChatCompletion(systemPrompt, chatHistory).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds system prompt for the persona.
    /// </summary>
    private string BuildSystemPrompt(Persona persona, string? generalTraining, List<(string TopicName, string Content)> topicContexts)
    {
        var promptParts = new List<string>
        {
            $"You are {persona.DisplayName}."
        };

        if (!string.IsNullOrWhiteSpace(persona.FirstName) || !string.IsNullOrWhiteSpace(persona.LastName))
        {
            var fullName = $"{persona.FirstName} {persona.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                promptParts.Add($"Your real name is {fullName}.");
            }
        }

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
        _personaManager?.Dispose();
        _messageManager?.Dispose();
        _chatManager?.Dispose();
    }
}
