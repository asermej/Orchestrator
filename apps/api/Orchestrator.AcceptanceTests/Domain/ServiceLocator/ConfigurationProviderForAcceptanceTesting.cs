using Microsoft.Extensions.Configuration;
using Orchestrator.Domain;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Configuration provider for acceptance tests. Loads appsettings.json from the test output directory
/// and forces Voice:UseFakeElevenLabs=true so that no real ElevenLabs API is ever called.
/// </summary>
internal sealed class ConfigurationProviderForAcceptanceTesting : ConfigurationProviderBase
{
    private const string VoiceUseFakeKey = "Voice:UseFakeElevenLabs";

    private readonly IConfigurationRoot _configurationRoot;

    public ConfigurationProviderForAcceptanceTesting()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false);
        _configurationRoot = builder.Build();
    }

    protected override string? RetrieveConfigurationSettingValue(string key)
    {
        if (string.Equals(key, VoiceUseFakeKey, StringComparison.OrdinalIgnoreCase))
        {
            return "true";
        }
        return _configurationRoot[key];
    }
}
