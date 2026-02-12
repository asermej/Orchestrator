using Orchestrator.Domain.DataLayer;

namespace Orchestrator.Domain;

/// <summary>
/// Manager for image upload and management operations
/// </summary>
internal class ImageManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private readonly ImageDataManager _imageDataManager;
    private readonly ImageValidator _validator;
    private bool _disposed = false;

    public ImageManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
        var configurationProvider = serviceLocator.CreateConfigurationProvider();
        _imageDataManager = new ImageDataManager(configurationProvider);
        _validator = new ImageValidator(configurationProvider);
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

