using System.Diagnostics.CodeAnalysis;

namespace Orchestrator.Domain;

[ExcludeFromCodeCoverage]
public sealed class ConfigurationSettingValueEmptyException : TechnicalBaseException
{
    public override string Reason => "Configuration Setting Value Empty";

    public ConfigurationSettingValueEmptyException()
    {
    }

    public ConfigurationSettingValueEmptyException(string message) : base(message)
    {
    }

    public ConfigurationSettingValueEmptyException(string message, Exception inner) : base(message, inner)
    {
    }
}