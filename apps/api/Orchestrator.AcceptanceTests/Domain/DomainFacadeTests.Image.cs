using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Image upload/delete tests. These tests require Azure Blob Storage configuration.
/// Tests will be skipped (Inconclusive) if Storage:BlobConnectionString is not configured.
/// </summary>
[TestClass]
public class DomainFacadeTests_Image
{
    private DomainFacade _domainFacade = null!;
    private string _testStoragePath = null!;
    private bool _blobStorageConfigured = false;

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        
        // Check if blob storage is configured
        try
        {
            var configProvider = serviceLocator.CreateConfigurationProvider();
            var blobConnectionString = configProvider.GetBlobConnectionString();
            _blobStorageConfigured = !string.IsNullOrWhiteSpace(blobConnectionString);
        }
        catch (ConfigurationSettingMissingException)
        {
            _blobStorageConfigured = false;
        }
        
        // Setup test storage directory
        _testStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "personas");
        if (Directory.Exists(_testStoragePath))
        {
            // Clean up any existing test files
            CleanupTestImages();
        }
    }
    
    private void SkipIfBlobStorageNotConfigured()
    {
        if (!_blobStorageConfigured)
        {
            Assert.Inconclusive("Azure Blob Storage is not configured. Set Storage:BlobConnectionString to run these tests.");
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            CleanupTestImages();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
        }
        finally
        {
            _domainFacade?.Dispose();
        }
    }

    [TestMethod]
    public async Task UploadImageAsync_ValidImage_ReturnsSuccessWithUrl()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "test-image.jpg",
            ContentType = "image/jpeg",
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };

        // Act
        var result = await _domainFacade.UploadImageAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.Url));
        Assert.IsFalse(string.IsNullOrEmpty(result.StoredFileName));
        Assert.IsTrue(result.Url.Contains(result.StoredFileName));
        Assert.IsTrue(result.UploadedAt <= DateTime.UtcNow);
        
        // Verify file exists on disk
        var filePath = Path.Combine(_testStoragePath, result.StoredFileName);
        Assert.IsTrue(File.Exists(filePath), "File should exist on disk after upload");
    }

    [TestMethod]
    public async Task UploadImageAsync_NullFileStream_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var request = new ImageUploadRequest
        {
            FileName = "test-image.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            FileStream = Stream.Null
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.UploadImageAsync(request)
        );
        
        Assert.IsTrue(exception.Message.Contains("No file provided"));
    }

    [TestMethod]
    public async Task UploadImageAsync_EmptyFileName_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "",
            ContentType = "image/jpeg",
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.UploadImageAsync(request)
        );
        
        Assert.IsTrue(exception.Message.Contains("Filename is required"));
    }

    [TestMethod]
    public async Task UploadImageAsync_FileSizeTooLarge_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "large-image.jpg",
            ContentType = "image/jpeg",
            FileSize = 11 * 1024 * 1024, // 11MB (exceeds 10MB limit)
            FileStream = new MemoryStream(imageBytes)
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.UploadImageAsync(request)
        );
        
        Assert.IsTrue(exception.Message.Contains("exceeds maximum allowed size"));
    }

    [TestMethod]
    public async Task UploadImageAsync_InvalidExtension_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "test-image.exe", // Invalid extension
            ContentType = "image/jpeg",
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.UploadImageAsync(request)
        );
        
        Assert.IsTrue(exception.Message.Contains("File type not allowed"));
    }

    [TestMethod]
    public async Task UploadImageAsync_InvalidContentType_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "test-image.jpg",
            ContentType = "application/pdf", // Invalid content type
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.UploadImageAsync(request)
        );
        
        Assert.IsTrue(exception.Message.Contains("Invalid file content type"));
    }

    [TestMethod]
    public async Task UploadImageAsync_PngImage_ReturnsSuccessWithUrl()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var request = new ImageUploadRequest
        {
            FileName = "test-image.png",
            ContentType = "image/png",
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };

        // Act
        var result = await _domainFacade.UploadImageAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.Url));
        Assert.IsTrue(result.StoredFileName.EndsWith(".png"));
        
        // Verify file exists on disk
        var filePath = Path.Combine(_testStoragePath, result.StoredFileName);
        Assert.IsTrue(File.Exists(filePath), "PNG file should exist on disk after upload");
    }

    [TestMethod]
    public async Task DeleteImageAsync_ExistingFile_DeletesSuccessfully()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange - First upload an image
        var imageBytes = CreateTestImageBytes();
        var uploadRequest = new ImageUploadRequest
        {
            FileName = "test-delete-image.jpg",
            ContentType = "image/jpeg",
            FileSize = imageBytes.Length,
            FileStream = new MemoryStream(imageBytes)
        };
        var uploadResult = await _domainFacade.UploadImageAsync(uploadRequest);
        var filePath = Path.Combine(_testStoragePath, uploadResult.StoredFileName);
        
        // Verify file exists before deletion
        Assert.IsTrue(File.Exists(filePath), "File should exist before deletion");

        // Act
        await _domainFacade.DeleteImageAsync(uploadResult.StoredFileName);

        // Assert
        Assert.IsFalse(File.Exists(filePath), "File should not exist after deletion");
    }

    [TestMethod]
    public async Task DeleteImageAsync_NonExistentFile_DoesNotThrowException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var fileName = "non-existent-file.jpg";

        // Act & Assert - Should not throw
        await _domainFacade.DeleteImageAsync(fileName);
        
        // If we get here, test passes
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task DeleteImageAsync_EmptyFileName_ThrowsValidationException()
    {
        SkipIfBlobStorageNotConfigured();
        
        // Arrange
        var fileName = "";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ImageValidationException>(
            async () => await _domainFacade.DeleteImageAsync(fileName)
        );
        
        Assert.IsTrue(exception.Message.Contains("Filename is required"));
    }

    // Helper Methods

    private byte[] CreateTestImageBytes()
    {
        // Create a simple test image (just some bytes that represent a minimal valid image structure)
        // For testing purposes, we don't need a real image
        var bytes = new byte[1024];
        new Random().NextBytes(bytes);
        return bytes;
    }

    private void CleanupTestImages()
    {
        if (Directory.Exists(_testStoragePath))
        {
            foreach (var file in Directory.GetFiles(_testStoragePath))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }
    }
}

