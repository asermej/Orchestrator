namespace Orchestrator.Domain;

/// <summary>
/// Validator for image upload operations
/// </summary>
internal class ImageValidator
{
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;
    private readonly string[] _allowedContentTypes;

    public ImageValidator(ConfigurationProviderBase configurationProvider)
    {
        _maxFileSize = configurationProvider.GetMaxImageFileSizeBytes();
        _allowedExtensions = configurationProvider.GetAllowedImageExtensions();
        _allowedContentTypes = configurationProvider.GetAllowedImageContentTypes();
    }

    /// <summary>
    /// Validates an image upload request
    /// </summary>
    /// <param name="request">The upload request to validate</param>
    /// <exception cref="ImageValidationException">Thrown when validation fails</exception>
    public void ValidateImageUpload(ImageUploadRequest request)
    {
        // Validate file exists
        if (request.FileStream == null || request.FileStream == Stream.Null)
        {
            throw new ImageValidationException("No file provided");
        }

        // Validate filename
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ImageValidationException("Filename is required");
        }

        // Validate file size
        if (request.FileSize <= 0)
        {
            throw new ImageValidationException("File size must be greater than zero");
        }

        if (request.FileSize > _maxFileSize)
        {
            var maxSizeMB = _maxFileSize / 1024 / 1024;
            throw new ImageValidationException($"File size exceeds maximum allowed size of {maxSizeMB}MB");
        }

        // Validate file extension
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            throw new ImageValidationException($"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        // Validate content type
        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ImageValidationException("Content type is required");
        }

        if (!_allowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            throw new ImageValidationException("Invalid file content type");
        }
    }
}

