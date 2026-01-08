namespace TentMan.Domain.Enums;

/// <summary>
/// Represents the storage provider for files.
/// </summary>
public enum StorageProvider
{
    Local = 1,
    AzureBlob = 2,
    S3 = 3
}
