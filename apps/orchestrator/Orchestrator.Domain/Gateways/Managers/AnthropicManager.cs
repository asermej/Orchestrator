using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed class AnthropicManager : IDisposable
{
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxRetries = 3;
    private static readonly int[] RetryDelaysMs = { 1000, 2000, 4000 };

    private static HttpClient? _sharedHttpClient;
    private static readonly object _httpClientLock = new();

    private readonly ServiceLocatorBase _serviceLocator;

    private HttpClient HttpClient
    {
        get
        {
            if (_sharedHttpClient != null) return _sharedHttpClient;
            lock (_httpClientLock)
            {
                if (_sharedHttpClient != null) return _sharedHttpClient;
                var config = _serviceLocator.CreateConfigurationProvider();
                var baseUrl = config.GetGatewayBaseUrl("Anthropic");
                var timeout = config.GetGatewayTimeout("Anthropic", 120);
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                };
                var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(timeout)
                };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", config.GetGatewayApiKey("Anthropic"));
                client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
                _sharedHttpClient = client;
            }
            return _sharedHttpClient;
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
        int? maxTokensOverride = null,
        bool enablePromptCaching = false,
        string? systemPromptInterviewPart = null)
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
                systemPromptInterviewPart,
                chatHistory,
                model,
                temperature,
                maxTokens,
                enablePromptCaching);

            LogCacheableBlockStructure(requestResource, model, enablePromptCaching);

            var jsonContent = JsonSerializer.Serialize(requestResource, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await HttpClient.PostAsync("/v1/messages", content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var statusCode = (int)response.StatusCode;

                    if (IsRetryableStatusCode(statusCode) && attempt < MaxRetries)
                    {
                        var delay = RetryDelaysMs[attempt];
                        Console.WriteLine($"[ANTHROPIC][RETRY] Transient error {statusCode}, attempt {attempt + 1}/{MaxRetries}, retrying in {delay}ms");
                        await Task.Delay(delay).ConfigureAwait(false);
                        continue;
                    }

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

            throw new AnthropicApiException("Anthropic API request failed after all retry attempts");
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        int? maxTokensOverride = null,
        string? modelOverride = null,
        bool enablePromptCaching = false,
        string? systemPromptInterviewPart = null)
    {
        var config = _serviceLocator.CreateConfigurationProvider();
        var model = modelOverride ?? config.GetGatewaySetting("Anthropic", "Model") ?? "claude-sonnet-4-6";
        var maxTokensStr = config.GetGatewaySetting("Anthropic", "MaxTokens");
        var temperatureStr = config.GetGatewaySetting("Anthropic", "Temperature");

        int maxTokens = maxTokensOverride ?? 4096;
        if (!maxTokensOverride.HasValue && !string.IsNullOrWhiteSpace(maxTokensStr) && int.TryParse(maxTokensStr, out var parsedMaxTokens))
            maxTokens = parsedMaxTokens;

        double temperature = 0.7;
        if (!string.IsNullOrWhiteSpace(temperatureStr) && double.TryParse(temperatureStr, out var parsedTemperature))
            temperature = parsedTemperature;

        var requestResource = AnthropicMapper.ToMessagesRequest(
            systemPrompt, systemPromptInterviewPart, chatHistory, model, temperature, maxTokens, enablePromptCaching);
        requestResource.Stream = true;

        LogCacheableBlockStructure(requestResource, model, enablePromptCaching);

        var jsonContent = JsonSerializer.Serialize(requestResource, new JsonSerializerOptions { WriteIndented = false });

        HttpResponseMessage? response = null;
        Stream? stream = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") { Content = content };
                response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var statusCode = (int)response.StatusCode;
                    response.Dispose();
                    response = null;

                    if (IsRetryableStatusCode(statusCode) && attempt < MaxRetries)
                    {
                        var delay = RetryDelaysMs[attempt];
                        Console.WriteLine($"[ANTHROPIC][RETRY] Transient streaming error {statusCode}, attempt {attempt + 1}/{MaxRetries}, retrying in {delay}ms");
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw new AnthropicApiException($"Anthropic streaming API returned error: {(System.Net.HttpStatusCode)statusCode} - {errorContent}");
                }

                stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (HttpRequestException ex)
            {
                response?.Dispose();
                response = null;
                if (attempt < MaxRetries)
                {
                    var delay = RetryDelaysMs[attempt];
                    Console.WriteLine($"[ANTHROPIC][RETRY] Connection error, attempt {attempt + 1}/{MaxRetries}, retrying in {delay}ms: {ex.Message}");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                throw new AnthropicConnectionException($"Failed to connect to Anthropic API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                response?.Dispose();
                response = null;
                if (attempt < MaxRetries)
                {
                    var delay = RetryDelaysMs[attempt];
                    Console.WriteLine($"[ANTHROPIC][RETRY] Timeout, attempt {attempt + 1}/{MaxRetries}, retrying in {delay}ms");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }
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
        }

        if (stream == null)
            throw new AnthropicApiException("Anthropic streaming API request failed after all retry attempts");

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

                if (evt?.Type == "message_start" && evt.Message?.Usage != null)
                {
                    var u = evt.Message.Usage;
                    var cacheHit = u.CacheReadInputTokens > 0 ? " (cache hit)" : "";
                    Console.WriteLine($"[ANTHROPIC][CACHE] input={u.InputTokens}, cache_create={u.CacheCreationInputTokens}, cache_read={u.CacheReadInputTokens}{cacheHit}");
                }

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

    /// <summary>
    /// Minimum cacheable prompt length (tokens) per model. Anthropic docs: 1024 for Sonnet 4.5/4, Opus 4.1/4; 2048 for Sonnet 4.6, Haiku 3.x; 4096 for Opus 4.6/4.5, Haiku 4.5.
    /// </summary>
    private static int GetMinimumCacheTokensForModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return 1024;
        var m = model.Trim();
        if (m.Contains("sonnet-4-6", StringComparison.OrdinalIgnoreCase)) return 2048;
        if (m.Contains("opus-4-6", StringComparison.OrdinalIgnoreCase) || m.Contains("opus-4-5", StringComparison.OrdinalIgnoreCase)) return 4096;
        if (m.Contains("haiku-4-5", StringComparison.OrdinalIgnoreCase)) return 4096;
        if (m.Contains("haiku-3", StringComparison.OrdinalIgnoreCase)) return 2048;
        return 1024;
    }

    /// <summary>
    /// Logs the structure of the system field for prompt-cache diagnostics.
    /// Anthropic requires cacheable block length >= model-specific minimum (e.g. 2048 for Claude Sonnet 4.6).
    /// </summary>
    private static void LogCacheableBlockStructure(AnthropicMessagesRequest requestResource, string model, bool enablePromptCaching)
    {
        if (!enablePromptCaching)
        {
            Console.WriteLine("[ANTHROPIC][CACHE-DIAG] prompt_caching=false, system sent as plain string");
            return;
        }

        var system = requestResource.System;
        if (system == null)
        {
            Console.WriteLine("[ANTHROPIC][CACHE-DIAG] system=null");
            return;
        }

        if (system is string s)
        {
            var approxTokens = s.Length / 4;
            Console.WriteLine($"[ANTHROPIC][CACHE-DIAG] system is string (not blocks) len={s.Length}, approx_tokens={approxTokens} — cache_control not applied");
            return;
        }

        if (system is List<AnthropicSystemBlock> blocks)
        {
            var minTokens = GetMinimumCacheTokensForModel(model);
            var totalCacheableTokens = 0;
            for (var i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                var approxTokens = b.Text?.Length / 4 ?? 0;
                var cacheType = b.CacheControl?.Type ?? "none";
                if (!string.IsNullOrEmpty(cacheType) && cacheType != "none")
                    totalCacheableTokens += approxTokens;
                Console.WriteLine($"[ANTHROPIC][CACHE-DIAG] system_block[{i}] type={b.Type}, len={b.Text?.Length ?? 0}, approx_tokens={approxTokens}, cache_control={cacheType}");
            }
            Console.WriteLine($"[ANTHROPIC][CACHE-DIAG] total_cacheable_approx_tokens={totalCacheableTokens} (model={model} requires >= {minTokens} for cache)");
            if (totalCacheableTokens < minTokens)
                Console.WriteLine($"[ANTHROPIC][CACHE-DIAG] CACHE WILL NOT ACTIVATE: {totalCacheableTokens} < {minTokens}. Expand system prompt to >= {minTokens} tokens or use a model with 1024 minimum (e.g. claude-sonnet-4-5).");
            for (var h = 0; h < blocks.Count; h++)
            {
                var text = blocks[h].Text ?? "";
                if (text.Length > 0)
                {
                    var hashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).AsSpan(0, 32).ToString();
                    Console.WriteLine($"[ANTHROPIC][SYSTEM-PROMPT] block[{h}] len={text.Length}, hash={hashHex} (identical hash across turns = cacheable)");
                }
            }
            return;
        }

        Console.WriteLine($"[ANTHROPIC][CACHE-DIAG] system type={system.GetType().Name}, unexpected");
    }

    private static bool IsRetryableStatusCode(int statusCode) =>
        statusCode is 429 or 500 or 502 or 503 or 529;

    public void Dispose()
    {
        // HttpClient is static/shared across all instances — do not dispose
    }
}
