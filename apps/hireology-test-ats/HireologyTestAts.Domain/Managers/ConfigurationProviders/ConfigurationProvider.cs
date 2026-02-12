using Microsoft.Extensions.Configuration;

namespace HireologyTestAts.Domain;

internal sealed class ConfigurationProvider : ConfigurationProviderBase
{
    private readonly IConfigurationRoot _configurationRoot;

    public ConfigurationProvider()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json");

        LoadEnvironmentSpecificAppSettings(configurationBuilder);

        _configurationRoot = configurationBuilder.Build();
    }

    internal ConfigurationProvider(IConfigurationRoot configurationRoot) => _configurationRoot = configurationRoot;

    protected override string? RetrieveConfigurationSettingValue(string key)
    {
        return _configurationRoot[key];
    }

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
        }
    }
}
