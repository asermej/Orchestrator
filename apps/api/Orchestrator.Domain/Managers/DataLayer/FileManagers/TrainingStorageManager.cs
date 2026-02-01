using System;
using System.IO;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Manages storage for training data with URL-based paths.
/// Currently supports: file:// (local filesystem)
/// Future support planned for: s3:// (AWS), https:// (HTTP), azure:// (Azure Blob Storage)
/// </summary>
internal sealed class TrainingStorageManager
{
    private const int MaxGeneralTrainingSize = 5000; // 5k chars (~1,250 tokens)
    private const int MaxTopicTrainingSize = 50000; // 50k chars (~12,500 tokens)
    private readonly string _basePath;

    public TrainingStorageManager()
    {
        // Base path for training data files
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "training-data", "personas");
        
        // Ensure directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <summary>
    /// Saves general training data for a persona to local filesystem
    /// </summary>
    /// <param name="personaId">The persona ID</param>
    /// <param name="content">The training content</param>
    /// <returns>The storage URL (file://) where the content was saved</returns>
    /// <exception cref="TrainingStorageException">Thrown when content exceeds size limit</exception>
    public async Task<string> SaveGeneralTraining(Guid personaId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            // If content is empty, just return empty URL (no file to save)
            return string.Empty;
        }

        if (content.Length > MaxGeneralTrainingSize)
        {
            throw new TrainingStorageException($"General training content exceeds maximum size of {MaxGeneralTrainingSize} characters (current: {content.Length})");
        }

        var fileName = $"{personaId}-general.txt";
        var filePath = Path.Combine(_basePath, fileName);
        
        await File.WriteAllTextAsync(filePath, content);
        
        // Return file:// URL
        // Normalize path separators and remove leading slash for proper file:/// URL format
        var normalizedPath = filePath.Replace("\\", "/").TrimStart('/');
        return $"file:///{normalizedPath}";
    }

    /// <summary>
    /// Saves topic training data for a persona to local filesystem
    /// </summary>
    /// <param name="personaId">The persona ID</param>
    /// <param name="topicId">The topic ID</param>
    /// <param name="content">The training content</param>
    /// <returns>The storage URL (file://) where the content was saved</returns>
    /// <exception cref="TrainingStorageException">Thrown when content exceeds size limit</exception>
    public async Task<string> SaveTopicTraining(Guid personaId, Guid topicId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            // If content is empty, just return empty URL (no file to save)
            return string.Empty;
        }

        if (content.Length > MaxTopicTrainingSize)
        {
            throw new TrainingStorageException($"Topic training content exceeds maximum size of {MaxTopicTrainingSize} characters (current: {content.Length})");
        }

        var fileName = $"{personaId}-topic-{topicId}.txt";
        var filePath = Path.Combine(_basePath, fileName);
        
        await File.WriteAllTextAsync(filePath, content);
        
        // Return file:// URL
        // Normalize path separators and remove leading slash for proper file:/// URL format
        var normalizedPath = filePath.Replace("\\", "/").TrimStart('/');
        var url = $"file:///{normalizedPath}";
        return url;
    }

    /// <summary>
    /// Gets training data from a URL
    /// Supports file:// (local filesystem), with future extensibility for s3://, https://, etc.
    /// </summary>
    /// <param name="url">The storage URL (file://, s3://, https://, etc.)</param>
    /// <returns>The training content, or empty string if URL is null/empty or resource not found</returns>
    public async Task<string> GetTrainingFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new TrainingStorageException($"Invalid URL format: {url}");
        }

        // Handle different URL schemes
        switch (uri.Scheme.ToLowerInvariant())
        {
            case "file":
                return await GetFromLocalFile(uri);
            
            // Future cloud storage support:
            // case "s3":
            //     return await GetFromS3(uri);
            // case "https":
            // case "http":
            //     return await GetFromHttp(uri);
            // case "azure":
            //     return await GetFromAzureBlob(uri);
            
            default:
                throw new TrainingStorageException(
                    $"Unsupported URL scheme '{uri.Scheme}'. " +
                    $"Currently supported: file://. " +
                    $"Future support planned for: s3://, https://, azure://");
        }
    }

    /// <summary>
    /// Retrieves content from local filesystem
    /// </summary>
    private async Task<string> GetFromLocalFile(Uri uri)
    {
        var filePath = uri.LocalPath;
        
        // Check if file exists
        if (!File.Exists(filePath))
        {
            // Return empty string instead of throwing - this is graceful degradation
            return string.Empty;
        }

        return await File.ReadAllTextAsync(filePath);
    }

    /// <summary>
    /// Deletes training data at the given URL
    /// </summary>
    /// <param name="url">The storage URL (file://, s3://, etc.)</param>
    public void DeleteTraining(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new TrainingStorageException($"Invalid URL format: {url}");
        }

        // Handle different URL schemes
        switch (uri.Scheme.ToLowerInvariant())
        {
            case "file":
                DeleteLocalFile(uri);
                break;
            
            // Future cloud storage support:
            // case "s3":
            //     DeleteFromS3(uri);
            //     break;
            // case "azure":
            //     DeleteFromAzureBlob(uri);
            //     break;
            
            default:
                throw new TrainingStorageException(
                    $"Unsupported URL scheme '{uri.Scheme}' for deletion. " +
                    $"Currently supported: file://");
        }
    }

    /// <summary>
    /// Deletes a file from local filesystem
    /// </summary>
    private void DeleteLocalFile(Uri uri)
    {
        var filePath = uri.LocalPath;
        
        // Delete file if it exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}

/// <summary>
/// Exception thrown when training storage operations fail
/// </summary>
public class TrainingStorageException : BusinessBaseException
{
    public override string Reason => "Training storage operation failed";

    public TrainingStorageException(string message) : base(message)
    {
    }

    public TrainingStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

