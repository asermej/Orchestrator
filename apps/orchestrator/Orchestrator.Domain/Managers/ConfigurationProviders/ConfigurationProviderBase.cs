namespace Orchestrator.Domain;

public abstract class ConfigurationProviderBase
{
    public string GetDbConnectionString()
    {
        return RetrieveConfigurationSettingValueThrowIfMissing("ConnectionStrings:DbConnectionString");
    }

    public string GetImageStoragePath()
    {
        var path = RetrieveConfigurationSettingValue("FileUpload:StoragePath");
        if (string.IsNullOrWhiteSpace(path))
        {
            // Default fallback path
            return Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "personas");
        }
        
        // If path is relative, make it absolute based on current directory
        if (!Path.IsPathRooted(path))
        {
            return Path.Combine(Directory.GetCurrentDirectory(), path);
        }
        
        return path;
    }

    public string GetImageBaseUrl()
    {
        var baseUrl = RetrieveConfigurationSettingValue("FileUpload:BaseUrl");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Default fallback URL
            return "/uploads/personas";
        }
        
        return baseUrl;
    }

    public long GetMaxImageFileSizeBytes()
    {
        var value = RetrieveConfigurationSettingValue("FileUpload:MaxFileSizeBytes");
        if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value, out var size))
        {
            // Default to 10MB
            return 10485760;
        }
        
        return size;
    }

    public string[] GetAllowedImageExtensions()
    {
        // Note: For array configuration, we'll need to handle this differently
        // For now, return defaults
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    }

    public string[] GetAllowedImageContentTypes()
    {
        return new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
    }

    /// <summary>
    /// Gets the Azure Blob Storage connection string from user-secrets or environment variables
    /// </summary>
    /// <returns>The blob storage connection string</returns>
    public string GetBlobConnectionString()
    {
        return RetrieveConfigurationSettingValueThrowIfMissing("Storage:BlobConnectionString");
    }

    /// <summary>
    /// Gets the Azure Blob Storage container name
    /// </summary>
    /// <returns>The container name, defaults to "images"</returns>
    public string GetBlobContainerName()
    {
        var containerName = RetrieveConfigurationSettingValue("Storage:BlobContainer");
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return "images";
        }
        return containerName;
    }

    /// <summary>
    /// Gets the Azure Blob Storage container name for interview audio recordings.
    /// </summary>
    /// <returns>The container name, defaults to "interview-recordings"</returns>
    public string GetInterviewRecordingsContainerName()
    {
        var containerName = RetrieveConfigurationSettingValue("Storage:InterviewRecordingsContainer");
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return "interview-recordings";
        }
        return containerName;
    }

    /// <summary>
    /// Gets the Azure Blob Storage container name for voice samples (private).
    /// </summary>
    /// <returns>The container name, defaults to "voice-samples"</returns>
    public string GetVoiceSamplesContainerName()
    {
        var containerName = RetrieveConfigurationSettingValue("Storage:VoiceSamplesContainer");
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return "voice-samples";
        }
        return containerName;
    }

    /// <summary>
    /// Gets the API key for a gateway integration
    /// </summary>
    /// <param name="integrationName">Name of the integration (e.g., "Llm", "Calendar")</param>
    /// <returns>API key value</returns>
    public string GetGatewayApiKey(string integrationName)
    {
        return RetrieveConfigurationSettingValueThrowIfMissing($"Gateways:{integrationName}:ApiKey");
    }

    /// <summary>
    /// Gets the base URL for a gateway integration
    /// </summary>
    /// <param name="integrationName">Name of the integration (e.g., "Llm", "Calendar")</param>
    /// <returns>Base URL value</returns>
    public string GetGatewayBaseUrl(string integrationName)
    {
        return RetrieveConfigurationSettingValueThrowIfMissing($"Gateways:{integrationName}:BaseUrl");
    }

    /// <summary>
    /// Gets the timeout in seconds for a gateway integration
    /// </summary>
    /// <param name="integrationName">Name of the integration</param>
    /// <param name="defaultTimeout">Default timeout if not configured (default: 30)</param>
    /// <returns>Timeout in seconds</returns>
    public int GetGatewayTimeout(string integrationName, int defaultTimeout = 30)
    {
        var value = RetrieveConfigurationSettingValue($"Gateways:{integrationName}:Timeout");
        if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var timeout))
        {
            return defaultTimeout;
        }
        return timeout;
    }

    /// <summary>
    /// Gets a configuration value for a gateway integration
    /// </summary>
    /// <param name="integrationName">Name of the integration</param>
    /// <param name="settingKey">Setting key (without the Gateways:{integrationName}: prefix)</param>
    /// <returns>Configuration value or null if not found</returns>
    public string? GetGatewaySetting(string integrationName, string settingKey)
    {
        return RetrieveConfigurationSettingValue($"Gateways:{integrationName}:{settingKey}");
    }

    /// <summary>
    /// Gets a configuration value by full key (e.g. "Voice:UseFakeElevenLabs").
    /// </summary>
    /// <param name="key">Full configuration key</param>
    /// <returns>Configuration value or null if not found</returns>
    public string? GetConfigurationValue(string key)
    {
        return RetrieveConfigurationSettingValue(key);
    }

    /// <summary>
    /// Gets the HMAC secret used for signing candidate session JWTs.
    /// Must be at least 32 characters for HS256.
    /// </summary>
    public string GetCandidateTokenSecret()
    {
        return RetrieveConfigurationSettingValueThrowIfMissing("CandidateSession:TokenSecret");
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
