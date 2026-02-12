using Microsoft.Extensions.Configuration;
using HireologyTestAts.Domain;

namespace HireologyTestAts.AcceptanceTests.Domain;

/// <summary>
/// Configuration provider for acceptance tests. Loads appsettings.json from the test output directory.
/// Forces the Orchestrator API key to empty so no real sync calls are made during tests.
/// </summary>
internal sealed class ConfigurationProviderForAcceptanceTesting : ConfigurationProviderBase
{
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
        // Disable Orchestrator sync in tests
        if (string.Equals(key, "HireologyAts:ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }
        return _configurationRoot[key];
    }
}
