using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages external API communication for OpenAI integration
/// Handles HTTP requests, error handling, and mapping between domain and resource models
/// </summary>
internal sealed class OpenAIManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private HttpClient? _httpClient;
    private HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                var config = _serviceLocator.CreateConfigurationProvider();
                var baseUrl = config.GetGatewayBaseUrl("OpenAI");
                var timeout = config.GetGatewayTimeout("OpenAI", 60);
                _httpClient = _serviceLocator.CreateHttpClient(baseUrl, timeout);
                
                // Set authentication header
                var apiKey = config.GetGatewayApiKey("OpenAI");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
            return _httpClient;
        }
    }

    public OpenAIManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    /// <summary>
    /// Generates a chat completion using OpenAI API
    /// </summary>
    /// <param name="systemPrompt">The system prompt that defines the persona behavior</param>
    /// <param name="chatHistory">Previous messages in the conversation</param>
    /// <returns>The AI-generated response content</returns>
    public async Task<string> GenerateChatCompletion(string systemPrompt, IEnumerable<Message> chatHistory)
    {
        try
        {
            var config = _serviceLocator.CreateConfigurationProvider();
            var model = config.GetGatewaySetting("OpenAI", "Model") ?? "gpt-3.5-turbo";
            var maxTokensStr = config.GetGatewaySetting("OpenAI", "MaxTokens");
            var temperatureStr = config.GetGatewaySetting("OpenAI", "Temperature");

            int maxTokens = 500;
            if (!string.IsNullOrWhiteSpace(maxTokensStr) && int.TryParse(maxTokensStr, out var parsedMaxTokens))
            {
                maxTokens = parsedMaxTokens;
            }

            double temperature = 0.7;
            if (!string.IsNullOrWhiteSpace(temperatureStr) && double.TryParse(temperatureStr, out var parsedTemperature))
            {
                temperature = parsedTemperature;
            }

            // Map domain model to resource model
            var requestResource = OpenAIMapper.ToChatCompletionRequest(
                systemPrompt,
                chatHistory,
                model,
                temperature,
                maxTokens);

            var jsonContent = JsonSerializer.Serialize(requestResource, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            
            // Make HTTP POST request to OpenAI
            HttpResponseMessage response = await HttpClient.PostAsync("/v1/chat/completions", content).ConfigureAwait(false);
            
            
            // Handle response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new OpenAIApiException($"OpenAI API returned error: {response.StatusCode} - {errorContent}");
            }

            // Parse response and extract content
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            if (responseResource == null)
            {
                throw new OpenAIApiException("Failed to deserialize OpenAI API response");
            }
            
            return OpenAIMapper.ExtractResponseContent(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new OpenAIConnectionException($"Failed to connect to OpenAI API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OpenAIConnectionException($"OpenAI API request timed out: {ex.Message}", ex);
        }
        catch (OpenAIApiException)
        {
            throw; // Re-throw gateway-specific exceptions
        }
        catch (OpenAIConnectionException)
        {
            throw; // Re-throw gateway-specific exceptions
        }
        catch (Exception ex)
        {
            throw new OpenAIApiException($"Unexpected error calling OpenAI API: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

