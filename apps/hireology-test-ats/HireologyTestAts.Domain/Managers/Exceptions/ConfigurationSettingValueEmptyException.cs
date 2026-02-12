using System.Diagnostics.CodeAnalysis;

namespace HireologyTestAts.Domain;

[ExcludeFromCodeCoverage]
public class ConfigurationSettingValueEmptyException : TechnicalBaseException
{
    public override string Reason => "A required configuration setting value is empty.";

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
