using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed class AnthropicManager : IDisposable
{
    private const string AnthropicVersion = "2023-06-01";

    private readonly ServiceLocatorBase _serviceLocator;
    private HttpClient? _httpClient;
    private HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                var config = _serviceLocator.CreateConfigurationProvider();
                var baseUrl = config.GetGatewayBaseUrl("Anthropic");
                var timeout = config.GetGatewayTimeout("Anthropic", 120);
                _httpClient = _serviceLocator.CreateHttpClient(baseUrl, timeout);

                var apiKey = config.GetGatewayApiKey("Anthropic");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
            }
            return _httpClient;
        }
    }

    public AnthropicManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    public async Task<string> GenerateCompletion(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        string? modelOverride = null,
        double? temperatureOverride = null,
        int? maxTokensOverride = null)
    {
        try
        {
            var config = _serviceLocator.CreateConfigurationProvider();
            var model = modelOverride ?? config.GetGatewaySetting("Anthropic", "Model") ?? "claude-sonnet-4-6";
            var maxTokensStr = config.GetGatewaySetting("Anthropic", "MaxTokens");
            var temperatureStr = config.GetGatewaySetting("Anthropic", "Temperature");

            int maxTokens = maxTokensOverride ?? 4096;
            if (!maxTokensOverride.HasValue && !string.IsNullOrWhiteSpace(maxTokensStr) && int.TryParse(maxTokensStr, out var parsedMaxTokens))
            {
                maxTokens = parsedMaxTokens;
            }

            double temperature = temperatureOverride ?? 0.7;
            if (!temperatureOverride.HasValue && !string.IsNullOrWhiteSpace(temperatureStr) && double.TryParse(temperatureStr, out var parsedTemperature))
            {
                temperature = parsedTemperature;
            }

            var requestResource = AnthropicMapper.ToMessagesRequest(
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

            HttpResponseMessage response = await HttpClient.PostAsync("/v1/messages", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new AnthropicApiException($"Anthropic API returned error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseResource = JsonSerializer.Deserialize<AnthropicMessagesResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (responseResource == null)
            {
                throw new AnthropicApiException("Failed to deserialize Anthropic API response");
            }

            return AnthropicMapper.ExtractResponseContent(responseResource);
        }
        catch (HttpRequestException ex)
        {
            throw new AnthropicConnectionException($"Failed to connect to Anthropic API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new AnthropicConnectionException($"Anthropic API request timed out: {ex.Message}", ex);
        }
        catch (AnthropicApiException)
        {
            throw;
        }
        catch (AnthropicConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AnthropicApiException($"Unexpected error calling Anthropic API: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt,
        IEnumerable<ConversationTurn> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = _serviceLocator.CreateConfigurationProvider();
        var model = config.GetGatewaySetting("Anthropic", "Model") ?? "claude-sonnet-4-6";
        var maxTokensStr = config.GetGatewaySetting("Anthropic", "MaxTokens");
        var temperatureStr = config.GetGatewaySetting("Anthropic", "Temperature");

        int maxTokens = 4096;
        if (!string.IsNullOrWhiteSpace(maxTokensStr) && int.TryParse(maxTokensStr, out var parsedMaxTokens))
            maxTokens = parsedMaxTokens;

        double temperature = 0.7;
        if (!string.IsNullOrWhiteSpace(temperatureStr) && double.TryParse(temperatureStr, out var parsedTemperature))
            temperature = parsedTemperature;

        var requestResource = AnthropicMapper.ToMessagesRequest(systemPrompt, chatHistory, model, temperature, maxTokens);
        requestResource.Stream = true;

        var jsonContent = JsonSerializer.Serialize(requestResource, new JsonSerializerOptions { WriteIndented = false });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        Stream? stream = null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") { Content = content };
            response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new AnthropicApiException($"Anthropic streaming API returned error: {response.StatusCode} - {errorContent}");
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            throw new AnthropicConnectionException($"Failed to connect to Anthropic API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            response?.Dispose();
            throw new AnthropicConnectionException($"Anthropic streaming API request timed out: {ex.Message}", ex);
        }
        catch (AnthropicApiException)
        {
            response?.Dispose();
            throw;
        }
        catch (OperationCanceledException)
        {
            response?.Dispose();
            throw;
        }

        using var reader = new StreamReader(stream);
        try
        {
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                    break;

                if (!line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6);

                AnthropicStreamingEvent? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<AnthropicStreamingEvent>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    continue;
                }

                if (evt?.Type == "message_stop")
                    break;

                if (evt?.Type == "content_block_delta" && evt.Delta?.Type == "text_delta")
                {
                    var token = evt.Delta.Text;
                    if (!string.IsNullOrEmpty(token))
                    {
                        yield return token;
                    }
                }
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
