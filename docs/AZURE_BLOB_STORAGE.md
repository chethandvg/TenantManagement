# Azure Blob Storage Configuration

This document explains how to configure Azure Blob Storage for file uploads in the TentMan application.

## Overview

The application uses Azure Blob Storage to store uploaded files such as tenant documents, building photos, and lease agreements. Files are stored securely with:

- **Container-based organization**: Files are organized into containers (e.g., `tenant-documents`)
- **Unique file names**: Each file is stored with a GUID-based name to prevent conflicts
- **Content-type headers**: Proper MIME types are set for file download
- **SHA256 hash verification**: File integrity is verified with cryptographic hashing

## Configuration

### Development (Local Storage Emulator)

For local development, you can use the Azure Storage Emulator (Azurite):

1. Install Azurite:
   ```bash
   npm install -g azurite
   ```

2. Start Azurite:
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. Configure in `appsettings.Development.json`:
   ```json
   {
     "AzureBlobStorage": {
       "ConnectionString": "UseDevelopmentStorage=true",
       "DefaultContainer": "tenant-documents"
     }
   }
   ```

### Production (Azure Blob Storage)

For production environments using actual Azure Blob Storage:

1. Create an Azure Storage Account in the Azure Portal

2. Get the connection string from the Azure Portal:
   - Go to your Storage Account
   - Select "Access keys" under Security + networking
   - Copy one of the connection strings

3. Configure in `appsettings.Production.json` or use environment variables:
   ```json
   {
     "AzureBlobStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net",
       "DefaultContainer": "tenant-documents"
     }
   }
   ```

   Or use environment variables:
   ```bash
   export AzureBlobStorage__ConnectionString="DefaultEndpointsProtocol=https;..."
   export AzureBlobStorage__DefaultContainer="tenant-documents"
   ```

### Configuration with Managed Identity (Recommended for Production)

For enhanced security in Azure environments, use Managed Identity instead of connection strings:

1. Enable Managed Identity on your Azure App Service or Function App

2. Grant the Managed Identity "Storage Blob Data Contributor" role on the Storage Account

3. Update the code to use DefaultAzureCredential instead of connection string (requires code modification)

## Container Structure

The application uses the following container structure:

- **tenant-documents**: Stores tenant-uploaded documents (ID proofs, address proofs, etc.)
- Storage key format: `container-name/blob-name`
  - Example: `tenant-documents/a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf`

## File Metadata

File metadata is stored in the database with:

- `StorageProvider`: Set to `AzureBlob` (enum value 2)
- `StorageKey`: The blob path (e.g., `tenant-documents/filename.pdf`)
- `FileName`: Original file name
- `ContentType`: MIME type
- `SizeBytes`: File size in bytes
- `Sha256`: SHA256 hash for integrity verification

## Security Considerations

1. **Private Containers**: All containers are created with private access (no public access)
2. **Signed URLs**: Future enhancement can add SAS tokens for temporary file access
3. **Connection String Protection**: Never commit connection strings to source control
4. **RBAC**: Use Azure RBAC and Managed Identity in production environments

## Troubleshooting

### Connection String Not Configured

If you see the error "Azure Blob Storage connection string is not configured", ensure:

1. The `AzureBlobStorage:ConnectionString` is set in appsettings or environment variables
2. For local development, Azurite is running
3. The connection string is correctly formatted

### Container Access Denied

Ensure the service principal or managed identity has the appropriate permissions:

- **Storage Blob Data Contributor** role for read/write operations
- **Storage Blob Data Reader** role for read-only operations

### File Upload Fails

Check:

1. File size doesn't exceed the 10MB limit
2. File type is in the allowed list (PDF, JPEG, PNG, DOC, DOCX)
3. Azure Storage account has sufficient quota
4. Network connectivity to Azure Storage

## Related Files

- **Service**: `src/TentMan.Infrastructure/Storage/AzureBlobStorageService.cs`
- **Interface**: `src/TentMan.Application/Abstractions/Storage/IFileStorageService.cs`
- **DI Registration**: `src/TentMan.Infrastructure/DependencyInjection.cs`
- **Command Handler**: `src/TentMan.Application/TenantManagement/TenantPortal/Commands/UploadTenantDocument/UploadTenantDocumentCommandHandler.cs`
