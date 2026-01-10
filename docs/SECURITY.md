# Security & Privacy Controls

This document outlines the security and privacy controls implemented in the TentMan application.

## Table of Contents

1. [Authorization & Access Control](#authorization--access-control)
2. [Data Masking & Privacy](#data-masking--privacy)
3. [Secure File Access](#secure-file-access)
4. [Audit Logging](#audit-logging)
5. [Security Best Practices](#security-best-practices)

## Authorization & Access Control

### Role-Based Access Control (RBAC)

The application implements a comprehensive RBAC system with the following roles:

#### Owner/Admin Roles
- **SuperAdmin**: Full system access with unrestricted permissions
- **Administrator**: Full CRUD operations on all resources within the organization
- **Manager**: Elevated permissions for managing resources and viewing audit logs

**Permissions:**
- Create, read, update, and delete leases
- Create, read, update, and delete lease terms
- Create, read, update, and delete deposit transactions
- Manage tenants and tenant data
- Access all files and documents
- View full (unmasked) sensitive data
- View audit logs

#### Tenant Role
- **Tenant**: Limited access to their own data only

**Permissions:**
- Read-only access to their own lease information
- Upload documents related to their lease
- View their own documents (masked sensitive data)
- Access files associated with their lease/handover
- Cannot modify lease terms or deposit transactions
- Cannot access other tenants' data

### Authorization Policies

#### LeaseAccessRequirement
Enforces access control for lease-related operations:
- **Admins/Managers**: Full access to all leases
- **Tenants**: Access only to leases where they are a party

**Implementation:**
```csharp
[Authorize(Policy = PolicyNames.LeaseAccess)]
public async Task<IActionResult> GetLease(Guid leaseId) { ... }
```

#### File Access Authorization
Files can only be accessed if:
- User is Admin/SuperAdmin/Manager (full access), OR
- User is the tenant who owns the file (via TenantDocument or UnitHandover), OR
- User is an owner of the building/unit associated with the file

## Data Masking & Privacy

### IDataMaskingService

Sensitive information is masked before being returned to unauthorized users.

### Masking Rules

#### Document Numbers
- **Format**: Shows last 4 characters only
- **Example**: `ABCD1234567890` → `****7890`
- **Applied to**: ID proofs, PAN cards, Aadhaar, etc.

```csharp
var masked = dataMaskingService.MaskDocumentNumber("ABCD1234567890");
// Returns: "****7890"
```

#### Email Addresses
- **Format**: Shows first character + 3 asterisks + domain
- **Example**: `user@example.com` → `u***@example.com`

```csharp
var masked = dataMaskingService.MaskEmail("user@example.com");
// Returns: "u***@example.com"
```

#### Phone Numbers
- **Format**: Shows country code + 5 asterisks + last 4 digits
- **Example**: `+1234567890` → `+12*****7890`

```csharp
var masked = dataMaskingService.MaskPhoneNumber("+1234567890");
// Returns: "+12*****7890"
```

### Authorization Check for Full Data Access

```csharp
var canViewFullData = dataMaskingService.CanViewFullData(currentUser.GetRoles());

if (!canViewFullData)
{
    // Return masked data
    response.DocumentNumber = dataMaskingService.MaskDocumentNumber(document.DocNumber);
}
else
{
    // Return full data for authorized users
    response.DocumentNumber = document.DocNumber;
}
```

## Secure File Access

### Short-Lived Signed URLs

Files are never accessed directly. Instead, the application generates short-lived signed URLs with the following characteristics:

- **Default Expiration**: 60 minutes (1 hour)
- **Maximum Expiration**: 1440 minutes (24 hours)
- **Technology**: Azure Blob Storage SAS (Shared Access Signatures)

#### Generating Signed URLs

```csharp
// Generate a signed URL for file access
GET /api/v1/files/{fileId}/signed-url?expiresInMinutes=60

Response:
{
  "fileId": "guid",
  "fileName": "document.pdf",
  "signedUrl": "https://storage.blob.core.windows.net/...",
  "expiresInMinutes": 60,
  "expiresAt": "2026-01-10T09:30:00Z"
}
```

### Proxied File Downloads

For smaller files or when signed URLs are not suitable, files can be downloaded through the API:

```csharp
// Download file through API with authorization check
GET /api/v1/files/{fileId}/download
```

**Authorization Flow:**
1. User requests file download/signed URL
2. API validates user authentication
3. API checks if user has permission to access the file
4. If authorized, generate signed URL or stream file
5. Log access attempt (success or denial)

### File Access Logging

All file access attempts are logged for audit purposes:
- Successful signed URL generation
- Successful file downloads
- Access denied attempts
- User information (ID, IP address, user agent)
- Timestamp

## Audit Logging

### Overview

The application automatically logs all changes to sensitive entities:
- **Lease**: All create, update, and delete operations
- **LeaseTerm**: All create, update, and delete operations
- **DepositTransaction**: All create, update, and delete operations

### Audit Log Structure

Each audit log entry contains:

| Field | Description |
|-------|-------------|
| `UserId` | The ID of the user who made the change |
| `UserName` | The username who made the change |
| `EntityType` | Type of entity (e.g., "Lease", "LeaseTerm") |
| `EntityId` | ID of the entity that was changed |
| `Action` | Action performed (Create, Update, Delete) |
| `BeforeState` | JSON of entity state before change (null for Create) |
| `AfterState` | JSON of entity state after change (null for Delete) |
| `ChangedProperties` | Comma-separated list of changed properties |
| `OrgId` | Organization ID associated with the change |
| `IpAddress` | IP address of the user |
| `UserAgent` | User agent of the client |
| `CreatedAtUtc` | Timestamp of the change |

### Implementation

Audit logging is implemented using an **EF Core Interceptor** (`AuditLoggingInterceptor`) that automatically captures changes during `SaveChangesAsync`.

#### Example Audit Log Entry

```json
{
  "id": "guid",
  "userId": "user-guid",
  "userName": "admin@example.com",
  "entityType": "Lease",
  "entityId": "lease-guid",
  "action": "Update",
  "beforeState": "{\"Status\": \"Draft\", \"MonthlyRent\": 1000}",
  "afterState": "{\"Status\": \"Active\", \"MonthlyRent\": 1200}",
  "changedProperties": "Status, MonthlyRent",
  "orgId": "org-guid",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0...",
  "createdAtUtc": "2026-01-10T08:00:00Z"
}
```

### Querying Audit Logs

Audit logs can be queried using the `IAuditLogRepository`:

```csharp
// Get audit logs for a specific entity
var logs = await auditLogRepository.GetByEntityAsync("Lease", leaseId);

// Get audit logs for an organization
var logs = await auditLogRepository.GetByOrganizationAsync(orgId, startDate, endDate);

// Get audit logs for a specific user
var logs = await auditLogRepository.GetByUserAsync(userId, startDate, endDate);
```

## Security Best Practices

### 1. Never Store Sensitive Data in Plain Text

- Document numbers in `TenantDocument` are stored in `DocNumberMasked` field (already masked)
- Full document numbers should never be stored in the database
- Use masking at data entry time

### 2. Always Use Authorization Checks

All endpoints that access sensitive data must:
- Require authentication (`[Authorize]` attribute)
- Check user roles or permissions
- Validate user has access to the specific resource

### 3. Audit All Sensitive Operations

The audit interceptor automatically logs changes to:
- Lease entities
- LeaseTerm entities
- DepositTransaction entities

Additional entities can be added by updating the `AuditedEntityTypes` set in `AuditLoggingInterceptor`.

### 4. Use Short-Lived Tokens

- File access URLs expire after 1 hour by default
- Maximum expiration is 24 hours
- Always validate authorization before generating signed URLs

### 5. Log Security Events

All security-relevant events are logged:
- Failed authorization attempts
- File access attempts (both successful and denied)
- Changes to sensitive data (via audit logs)

### 6. Implement Defense in Depth

Multiple layers of security:
1. JWT authentication (API level)
2. Role-based authorization (Policy level)
3. Resource-specific authorization (Handler level)
4. Audit logging (Database level)

## Configuration

### Enable Audit Logging

Audit logging is automatically enabled when the `AuditLoggingInterceptor` is registered in the DI container (already configured in `DependencyInjection.cs`).

### Configure File Storage

Set up Azure Blob Storage connection in `appsettings.json`:

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "your-connection-string",
    "DefaultContainer": "tenant-documents"
  }
}
```

### Configure JWT Authentication

JWT settings in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters",
    "Issuer": "TentMan",
    "Audience": "TentMan-API",
    "ExpiresInMinutes": 60,
    "RefreshTokenExpiresInDays": 7
  }
}
```

## Testing Security Controls

### Unit Tests

Test files:
- `AuditLoggingInterceptorTests.cs` - Tests for audit logging
- `DataMaskingServiceTests.cs` - Tests for data masking
- `LeaseAccessRequirementHandlerTests.cs` - Tests for authorization

### Integration Tests

Test scenarios:
- Tenant can only access their own leases
- Admin can access all leases
- File access is restricted by authorization
- Audit logs are created for all changes

## Compliance

### Data Protection

- **GDPR Compliance**: Sensitive data is masked and access is logged
- **Data Minimization**: Only necessary data is collected and stored
- **Right to Access**: Users can request their audit logs
- **Right to Erasure**: Soft delete preserves audit trail while removing active data

### Security Standards

- **OWASP Top 10**: Protection against common web vulnerabilities
- **Authentication**: JWT-based authentication with refresh tokens
- **Authorization**: Role-based and resource-based access control
- **Audit Trail**: Complete logging of all changes to sensitive data

## Troubleshooting

### Common Issues

#### 1. File Access Denied

**Symptom**: User receives 403 Forbidden when accessing a file

**Solutions**:
- Verify user has correct role (Admin/Manager/Tenant)
- Verify user is associated with the file (tenant documents, lease handover, etc.)
- Check audit logs for access attempt details

#### 2. Data Still Visible (Not Masked)

**Symptom**: Sensitive data is visible to unauthorized users

**Solutions**:
- Verify `IDataMaskingService` is called in query handlers
- Check user roles using `CanViewFullData()`
- Ensure DTOs are using masked values for non-admin users

#### 3. Audit Logs Not Created

**Symptom**: Changes are not being logged

**Solutions**:
- Verify `AuditLoggingInterceptor` is registered
- Check entity type is in `AuditedEntityTypes` set
- Ensure `SaveChangesAsync` is being called
- Check database migration for `AuditLogs` table

## Future Enhancements

Potential security improvements:
- [ ] Two-factor authentication (2FA)
- [ ] Rate limiting on file access endpoints
- [ ] Automated security scanning (SAST/DAST)
- [ ] Encryption at rest for sensitive fields
- [ ] Advanced threat detection
- [ ] Security headers (CSP, HSTS, etc.)

## Contact

For security concerns or to report vulnerabilities, please contact the security team.

---

**Last Updated**: 2026-01-10
**Version**: 1.0
