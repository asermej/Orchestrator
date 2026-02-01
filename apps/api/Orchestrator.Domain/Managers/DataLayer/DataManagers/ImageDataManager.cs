using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Orchestrator.Domain.DataLayer;

/// <summary>
/// Data manager responsible for image storage operations using Azure Blob Storage
/// </summary>
internal class ImageDataManager
{
    private readonly BlobContainerClient _containerClient;
    private const string ImageApiBasePath = "/api/v1/images";

    public ImageDataManager(ConfigurationProviderBase configurationProvider)
    {
        var connectionString = configurationProvider.GetBlobConnectionString();
        var containerName = configurationProvider.GetBlobContainerName();
        
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        
        // Ensure container exists (creates if not present)
        _containerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Saves an image to Azure Blob Storage
    /// </summary>
    /// <param name="request">The image upload request containing file data</param>
    /// <returns>The result containing the API URL and storage information</returns>
    public async Task<ImageUploadResult> SaveImageAsync(ImageUploadRequest request)
    {
        try
        {
            // Generate unique blob key
            var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            var blobKey = $"{Guid.NewGuid()}{extension}";

            var blobClient = _containerClient.GetBlobClient(blobKey);

            // Set content type for proper serving
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = request.ContentType
            };

            // Upload to blob storage
            await blobClient.UploadAsync(request.FileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            // Return API URL (not blob URL - container is private)
            var url = $"{ImageApiBasePath}/{blobKey}";

            return new ImageUploadResult
            {
                Url = url,
                StoredFileName = blobKey,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            throw new ImageUploadException($"Failed to save image to blob storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves an image from Azure Blob Storage
    /// </summary>
    /// <param name="key">The blob key (filename)</param>
    /// <returns>The image download result, or null if not found</returns>
    public async Task<ImageDownloadResult?> GetImageAsync(string key)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);
            
            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync();
            var properties = await blobClient.GetPropertiesAsync();

            return new ImageDownloadResult
            {
                Stream = response.Value.Content,
                ContentType = properties.Value.ContentType ?? GetContentTypeFromExtension(key),
                Key = key
            };
        }
        catch (Exception ex)
        {
            throw new ImageUploadException($"Failed to retrieve image from blob storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes an image from Azure Blob Storage
    /// </summary>
    /// <param name="key">The blob key (filename) to delete</param>
    public async Task DeleteImageAsync(string key)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);
            await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            throw new ImageUploadException($"Failed to delete image from blob storage: {ex.Message}", ex);
        }
    }

    private static string GetContentTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

