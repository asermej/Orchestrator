namespace HireologyTestAts.Domain;

internal sealed class ServiceLocator : ServiceLocatorBase
{
    protected override ConfigurationProviderBase CreateConfigurationProviderCore()
    {
        return new ConfigurationProvider();
    }
}
