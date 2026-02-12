using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Orchestrator.Domain;

internal sealed class ConfigurationProvider : ConfigurationProviderBase
{
    private readonly IConfigurationRoot _configurationRoot;

    // UserSecretsId from Platform.Api.csproj - must match the API project's UserSecretsId
    private const string UserSecretsId = "b1334b12-f5d8-4a4a-baf3-046c52eeb073";

    public ConfigurationProvider()
    {
        var configurationBuilder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json");

        LoadEnvironmentSpecificAppSettings(configurationBuilder);

        _configurationRoot = configurationBuilder.Build();
    }

    protected override string RetrieveConfigurationSettingValue(string key)
    {
        return _configurationRoot[key]!;
    }

    internal ConfigurationProvider(IConfigurationRoot configurationRoot) => _configurationRoot = configurationRoot;


    private static void LoadEnvironmentSpecificAppSettings(IConfigurationBuilder configurationBuilder)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(environment))
        {
            var envFile = $"appsettings.{environment}.json";
            if (File.Exists(envFile))
            {
                configurationBuilder.AddJsonFile(envFile);
            }

            // Load user secrets in Development environment
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                configurationBuilder.AddUserSecrets(UserSecretsId);
            }
        }
    }
}
