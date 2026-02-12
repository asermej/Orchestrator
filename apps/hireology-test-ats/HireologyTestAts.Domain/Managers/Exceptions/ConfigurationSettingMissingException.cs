using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class ConfigurationSettingMissingException : TechnicalBaseException
{
    public override string Reason => "A required configuration setting is missing.";

    public ConfigurationSettingMissingException()
    {
    }

    public ConfigurationSettingMissingException(string message) : base(message)
    {
    }

    public ConfigurationSettingMissingException(string message, Exception inner) : base(message, inner)
    {
    }
}
