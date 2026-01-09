using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TentMan.Application.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation of IFileStorageService.
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadFileAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading file {FileName} to Azure Blob Storage container {Container}", fileName, containerName);

        try
        {
            // Get or create container
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            // Generate unique blob name
            var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set blob headers
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            // Upload the file
            stream.Position = 0;
            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                },
                cancellationToken);

            // Return the storage key (container/blobname)
            var storageKey = $"{containerName}/{blobName}";
            _logger.LogInformation("File {FileName} uploaded successfully to {StorageKey}", fileName, storageKey);

            return storageKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to Azure Blob Storage", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading file from Azure Blob Storage: {StorageKey}", storageKey);

        try
        {
            var (containerName, blobName) = ParseStorageKey(storageKey);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadAsync(cancellationToken);
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogInformation("File downloaded successfully from {StorageKey}", storageKey);
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Azure Blob Storage: {StorageKey}", storageKey);
            throw;
        }
    }

    public async Task DeleteFileAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file from Azure Blob Storage: {StorageKey}", storageKey);

        try
        {
            var (containerName, blobName) = ParseStorageKey(storageKey);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("File deleted successfully from {StorageKey}", storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Azure Blob Storage: {StorageKey}", storageKey);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (containerName, blobName) = ParseStorageKey(storageKey);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists in Azure Blob Storage: {StorageKey}", storageKey);
            return false;
        }
    }

    private static (string containerName, string blobName) ParseStorageKey(string storageKey)
    {
        var parts = storageKey.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid storage key format: {storageKey}. Expected format: container/blobname");
        }

        return (parts[0], parts[1]);
    }
}

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    /// <summary>
    /// Azure Storage connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Default container name for tenant documents.
    /// </summary>
    public string DefaultContainer { get; set; } = "tenant-documents";
}
