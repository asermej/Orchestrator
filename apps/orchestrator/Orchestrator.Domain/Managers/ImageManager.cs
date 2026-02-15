using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Orchestrator.Domain.DataLayer;

namespace Orchestrator.Domain;

/// <summary>
/// Manager for image upload and management operations, including interview audio recordings
/// </summary>
internal class ImageManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private readonly ImageDataManager _imageDataManager;
    private readonly ImageValidator _validator;
    private readonly ConfigurationProviderBase _configurationProvider;
    private bool _disposed = false;

    public ImageManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
        _configurationProvider = serviceLocator.CreateConfigurationProvider();
        _imageDataManager = new ImageDataManager(_configurationProvider);
        _validator = new ImageValidator(_configurationProvider);
    }

    /// <summary>
    /// Uploads an image file
    /// </summary>
    /// <param name="request">The image upload request</param>
    /// <returns>The upload result with URL and metadata</returns>
    public async Task<ImageUploadResult> UploadImageAsync(ImageUploadRequest request)
    {
        // Validate the upload request
        _validator.ValidateImageUpload(request);

        // Save the image
        var result = await _imageDataManager.SaveImageAsync(request);

        return result;
    }

    /// <summary>
    /// Deletes an image file
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    public async Task DeleteImageAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ImageValidationException("Filename is required for deletion");
        }

        await _imageDataManager.DeleteImageAsync(fileName);
    }

    /// <summary>
    /// Retrieves an image by key
    /// </summary>
    /// <param name="key">The image key (filename)</param>
    /// <returns>The image download result, or null if not found</returns>
    public async Task<ImageDownloadResult?> GetImageAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return await _imageDataManager.GetImageAsync(key);
    }

    /// <summary>
    /// Uploads interview audio to Azure Blob Storage
    /// </summary>
    /// <param name="audioStream">The audio stream to upload</param>
    /// <param name="contentType">The MIME type (e.g., audio/webm)</param>
    /// <returns>The API-relative URL of the uploaded audio</returns>
    public async Task<string> UploadInterviewAudioAsync(Stream audioStream, string contentType)
    {
        if (audioStream == null || audioStream.Length == 0)
        {
            throw new ImageValidationException("No audio data provided");
        }

        var connectionString = _configurationProvider.GetBlobConnectionString();
        var containerName = _configurationProvider.GetInterviewRecordingsContainerName();
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var extension = contentType.Contains("webm") ? ".webm" : ".mp3";
        var blobKey = $"{Guid.NewGuid()}{extension}";
        var blobClient = containerClient.GetBlobClient(blobKey);

        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(audioStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

        return $"/api/v1/interview-audio/{blobKey}";
    }

    /// <summary>
    /// Retrieves interview audio from Azure Blob Storage
    /// </summary>
    /// <param name="key">The blob key (filename)</param>
    /// <returns>The audio stream and content type, or null if not found</returns>
    public async Task<(Stream Stream, string ContentType)?> GetInterviewAudioAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var connectionString = _configurationProvider.GetBlobConnectionString();
        var containerName = _configurationProvider.GetInterviewRecordingsContainerName();
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(key);

        if (!await blobClient.ExistsAsync()) return null;

        var response = await blobClient.DownloadStreamingAsync();
        var properties = await blobClient.GetPropertiesAsync();
        return (response.Value.Content, properties.Value.ContentType ?? "audio/webm");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // ImageDataManager doesn't implement IDisposable
            _disposed = true;
        }
    }
}

