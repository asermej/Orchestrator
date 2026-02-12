namespace HireologyTestAts.Domain;

public abstract class ConfigurationProviderBase
{
    public string GetDbConnectionString()
    {
        return RetrieveConfigurationSettingValueThrowIfMissing("ConnectionStrings:HireologyTestAts");
    }

    public string GetOrchestratorBaseUrl()
    {
        var value = RetrieveConfigurationSettingValue("HireologyAts:BaseUrl");
        if (string.IsNullOrWhiteSpace(value))
        {
            return "http://localhost:5000";
        }
        return value.TrimEnd('/');
    }

    public string GetOrchestratorApiKey()
    {
        return RetrieveConfigurationSettingValue("HireologyAts:ApiKey") ?? string.Empty;
    }

    private string RetrieveConfigurationSettingValueThrowIfMissing(string key)
    {
        var valueAsRetrieved = RetrieveConfigurationSettingValue(key);
        if (valueAsRetrieved == null)
        {
            throw new ConfigurationSettingMissingException($"The Configuration Setting with Key: {key}, is Missing from the Configuration file");
        }
        else if (valueAsRetrieved.Length == 0)
        {
            throw new ConfigurationSettingValueEmptyException($"The Configuration Setting with Key: {key}, Exists but its value is Empty");
        }
        else if (IsWhiteSpaces(valueAsRetrieved))
        {
            throw new ConfigurationSettingValueEmptyException($"The Configuration Setting with Key: {key}, Exists but its value is White spaces.");
        }

        return valueAsRetrieved;
    }

    private static bool IsWhiteSpaces(string valueAsRetrieved)
    {
        foreach (var chr in valueAsRetrieved)
        {
            if (!char.IsWhiteSpace(chr))
            {
                return false;
            }
        }

        return true;
    }

    protected abstract string? RetrieveConfigurationSettingValue(string key);
}
