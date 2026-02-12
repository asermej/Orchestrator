using System.Net.Http;

namespace HireologyTestAts.Domain;

internal abstract class ServiceLocatorBase
{
    public ConfigurationProviderBase CreateConfigurationProvider()
    {
        return CreateConfigurationProviderCore();
    }

    protected abstract ConfigurationProviderBase CreateConfigurationProviderCore();

    /// <summary>
    /// Creates an HttpClient instance configured for external API calls
    /// </summary>
    /// <param name="baseUrl">Base URL for the external API</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30)</param>
    /// <returns>Configured HttpClient instance</returns>
    public HttpClient CreateHttpClient(string baseUrl, int timeoutSeconds = 30)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }
}
