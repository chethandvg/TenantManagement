namespace TentMan.Application.Abstractions.Storage;

/// <summary>
/// Interface for file storage operations across different storage providers.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the storage key.
    /// </summary>
    /// <param name="stream">The file stream to upload.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="containerName">The container/folder name for organizing files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage key/path where the file is stored.</returns>
    Task<string> UploadFileAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file stream.</returns>
    Task<Stream> DownloadFileAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
