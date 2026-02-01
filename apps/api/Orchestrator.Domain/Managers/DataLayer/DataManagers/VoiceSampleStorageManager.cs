using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Orchestrator.Domain;

/// <summary>
/// Uploads voice sample audio to Azure Blob Storage (private voice-samples container) for audit.
/// </summary>
internal sealed class VoiceSampleStorageManager
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;

    public VoiceSampleStorageManager(ConfigurationProviderBase configurationProvider)
    {
        var connectionString = configurationProvider.GetBlobConnectionString();
        _containerName = configurationProvider.GetVoiceSamplesContainerName();
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        _containerClient.CreateIfNotExists();
    }

    /// <summary>
    /// Uploads voice sample bytes to the voice-samples container. Returns a blob path for audit (e.g. voice-samples/guid.ext).
    /// </summary>
    public async Task<string> UploadAsync(byte[] bytes, string fileName, string contentType, System.Threading.CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".mp3";
        }
        var blobName = $"{Guid.NewGuid()}{extension}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType ?? "audio/mpeg"
        };

        using var stream = new MemoryStream(bytes);
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken).ConfigureAwait(false);

        return $"{_containerName}/{blobName}";
    }
}
