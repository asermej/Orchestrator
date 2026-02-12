using HireologyTestAts.Domain;

namespace HireologyTestAts.AcceptanceTests.Domain;

internal sealed class ServiceLocatorForAcceptanceTesting : ServiceLocatorBase
{
    protected override ConfigurationProviderBase CreateConfigurationProviderCore()
    {
        return new ConfigurationProviderForAcceptanceTesting();
    }
}
