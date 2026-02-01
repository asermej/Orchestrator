using Orchestrator.Domain;

namespace Orchestrator.AcceptanceTests.Domain;

internal sealed class ServiceLocatorForAcceptanceTesting : ServiceLocatorBase
{
    protected override ConfigurationProviderBase CreateConfigurationProviderCore()
    {
        return new ConfigurationProviderForAcceptanceTesting();
    }
}
