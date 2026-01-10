using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Storage;
using TentMan.Contracts.Common;
using TentMan.Infrastructure.Persistence;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Api.Controllers;

/// <summary>
/// API endpoints for secure file access with authorization and audit logging.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService fileStorageService,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogRepository auditLogRepository,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generates a short-lived signed URL for secure file access.
    /// Requires authorization and logs access attempts.
    /// </summary>
    /// <param name="fileId">The file metadata ID.</param>
    /// <param name="expiresInMinutes">URL expiration time in minutes (default: 60, max: 1440 = 24 hours).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A signed URL for temporary file access.</returns>
    [HttpGet("{fileId:guid}/signed-url")]
    [ProducesResponseType(typeof(ApiResponse<FileAccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FileAccessResponse>>> GetSignedUrl(
        Guid fileId,
        [FromQuery] int expiresInMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        // Validate expiration time
        if (expiresInMinutes < 1 || expiresInMinutes > 1440)
        {
            return BadRequest(ApiResponse<object>.Fail("Expiration time must be between 1 and 1440 minutes (24 hours)"));
        }

        _logger.LogInformation("User {UserId} requesting signed URL for file {FileId}", _currentUser.UserId, fileId);

        // Get file metadata
        var fileMetadata = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (fileMetadata == null)
        {
            _logger.LogWarning("File {FileId} not found", fileId);
            return NotFound(ApiResponse<object>.Fail($"File {fileId} not found"));
        }

        // Check authorization - user must have access to the file
        var hasAccess = await CheckFileAccessAsync(fileId, cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} denied access to file {FileId}", _currentUser.UserId, fileId);
            
            // Log unauthorized access attempt
            await LogFileAccessAttemptAsync(fileId, "AccessDenied", cancellationToken);
            
            return Forbid();
        }

        try
        {
            // Generate signed URL
            var signedUrl = await _fileStorageService.GenerateSignedUrlAsync(
                fileMetadata.StorageKey,
                expiresInMinutes,
                cancellationToken);

            // Log successful access
            await LogFileAccessAttemptAsync(fileId, "SignedUrlGenerated", cancellationToken);

            var response = new FileAccessResponse
            {
                FileId = fileId,
                FileName = fileMetadata.FileName,
                SignedUrl = signedUrl,
                ExpiresInMinutes = expiresInMinutes,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes)
            };

            _logger.LogInformation("Signed URL generated for file {FileId}, expires in {Minutes} minutes", fileId, expiresInMinutes);

            return Ok(ApiResponse<FileAccessResponse>.Ok(response, "Signed URL generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signed URL for file {FileId}", fileId);
            return StatusCode(500, ApiResponse<object>.Fail("Error generating file access URL"));
        }
    }

    /// <summary>
    /// Downloads a file directly (proxied through API with authorization).
    /// Use this for smaller files or when signed URLs are not suitable.
    /// </summary>
    [HttpGet("{fileId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} requesting download for file {FileId}", _currentUser.UserId, fileId);

        // Get file metadata
        var fileMetadata = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (fileMetadata == null)
        {
            _logger.LogWarning("File {FileId} not found", fileId);
            return NotFound();
        }

        // Check authorization
        var hasAccess = await CheckFileAccessAsync(fileId, cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} denied access to file {FileId}", _currentUser.UserId, fileId);
            await LogFileAccessAttemptAsync(fileId, "DownloadDenied", cancellationToken);
            return Forbid();
        }

        try
        {
            // Download file from storage
            var fileStream = await _fileStorageService.DownloadFileAsync(
                fileMetadata.StorageKey,
                cancellationToken);

            // Log successful download
            await LogFileAccessAttemptAsync(fileId, "FileDownloaded", cancellationToken);

            _logger.LogInformation("File {FileId} downloaded by user {UserId}", fileId, _currentUser.UserId);

            return File(fileStream, fileMetadata.ContentType, fileMetadata.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Checks if the current user has access to a specific file.
    /// Access is granted if:
    /// - User is Admin/SuperAdmin/Manager (full access)
    /// - User is the tenant who owns the file (via TenantDocument or UnitHandover)
    /// - User is an owner of the building/unit associated with the file
    /// </summary>
    private async Task<bool> CheckFileAccessAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var userRoles = _currentUser.GetRoles().ToList();

        // Admins have full access
        if (_currentUser.IsInRole("Administrator") ||
            _currentUser.IsInRole("SuperAdmin") ||
            _currentUser.IsInRole("Manager"))
        {
            return true;
        }

        if (string.IsNullOrEmpty(_currentUser.UserId) || !Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return false;
        }

        // Check if file is associated with tenant's documents
        var isTenantDocument = await _dbContext.TenantDocuments
            .Include(td => td.Tenant)
            .AnyAsync(td => td.FileId == fileId && td.Tenant.LinkedUserId == userId, cancellationToken);

        if (isTenantDocument)
        {
            return true;
        }

        // Check if file is associated with a handover for a lease the user is party to
        var isHandoverFile = await _dbContext.UnitHandovers
            .Include(uh => uh.Lease)
            .ThenInclude(l => l.Parties)
            .ThenInclude(lp => lp.Tenant)
            .AnyAsync(uh => (uh.SignatureTenantFileId == fileId || uh.SignatureOwnerFileId == fileId) &&
                           uh.Lease.Parties.Any(lp => lp.Tenant.LinkedUserId == userId),
                      cancellationToken);

        if (isHandoverFile)
        {
            return true;
        }

        // Check if file is associated with building/unit files that the user owns
        var isBuildingFile = await _dbContext.BuildingFiles
            .Include(bf => bf.Building)
            .ThenInclude(b => b.OwnershipShares)
            .AnyAsync(bf => bf.FileId == fileId &&
                           bf.Building.OwnershipShares.Any(os => os.Owner.Id == userId),
                      cancellationToken);

        if (isBuildingFile)
        {
            return true;
        }

        var isUnitFile = await _dbContext.UnitFiles
            .Include(uf => uf.Unit)
            .ThenInclude(u => u.OwnershipShares)
            .AnyAsync(uf => uf.FileId == fileId &&
                           uf.Unit.OwnershipShares.Any(os => os.Owner.Id == userId),
                      cancellationToken);

        return isUnitFile;
    }

    /// <summary>
    /// Logs file access attempts for audit purposes.
    /// </summary>
    private async Task LogFileAccessAttemptAsync(Guid fileId, string action, CancellationToken cancellationToken)
    {
        try
        {
            var auditLog = new Domain.Entities.AuditLog
            {
                UserId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null,
                UserName = _currentUser.UserId,
                EntityType = "FileMetadata",
                EntityId = fileId,
                Action = action,
                IpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request?.Headers["User-Agent"].ToString(),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging file access attempt for file {FileId}", fileId);
            // Don't fail the request if audit logging fails
        }
    }
}

/// <summary>
/// Response model for file access with signed URL.
/// </summary>
public class FileAccessResponse
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SignedUrl { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
    public DateTime ExpiresAt { get; set; }
}
