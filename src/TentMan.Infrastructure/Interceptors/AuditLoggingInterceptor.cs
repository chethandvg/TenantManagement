using TentMan.Application.Abstractions;
using TentMan.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace TentMan.Infrastructure.Interceptors;

/// <summary>
/// Interceptor that automatically creates audit log entries for changes to sensitive entities.
/// Tracks changes to Lease, LeaseTerm, and DepositTransaction entities.
/// </summary>
public class AuditLoggingInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly ITimeProvider _timeProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // Entity types that should be audited
    private static readonly HashSet<Type> AuditedEntityTypes = new()
    {
        typeof(Lease),
        typeof(LeaseTerm),
        typeof(DepositTransaction)
    };

    public AuditLoggingInterceptor(
        ICurrentUser currentUser,
        ITimeProvider timeProvider,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            CreateAuditLogs(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext context)
    {
        var auditLogs = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditedEntityTypes.Contains(e.Entity.GetType()) &&
                       (e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        if (auditLogs.Any())
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);

        if (entityId == Guid.Empty)
        {
            return null;
        }

        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => null
        };

        if (action == null)
        {
            return null;
        }

        var userId = _currentUser.UserId != null && Guid.TryParse(_currentUser.UserId, out var uid) 
            ? uid 
            : (Guid?)null;

        var httpContext = _httpContextAccessor?.HttpContext;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

        var auditLog = new AuditLog
        {
            UserId = userId,
            UserName = _currentUser.UserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OrgId = GetOrgId(entry),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = _timeProvider.UtcNow,
            CreatedBy = _currentUser.UserId ?? "System"
        };

        if (entry.State == EntityState.Added)
        {
            auditLog.AfterState = SerializeEntity(entry, PropertyValueType.CurrentValue);
        }
        else if (entry.State == EntityState.Deleted)
        {
            auditLog.BeforeState = SerializeEntity(entry, PropertyValueType.OriginalValue);
        }
        else if (entry.State == EntityState.Modified)
        {
            var changedProperties = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (changedProperties.Any())
            {
                auditLog.ChangedProperties = string.Join(", ", changedProperties);
                auditLog.BeforeState = SerializeEntity(entry, PropertyValueType.OriginalValue);
                auditLog.AfterState = SerializeEntity(entry, PropertyValueType.CurrentValue);
            }
        }

        return auditLog;
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        if (idProperty?.CurrentValue is Guid id)
        {
            return id;
        }
        return Guid.Empty;
    }

    private static Guid? GetOrgId(EntityEntry entry)
    {
        var orgIdProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "OrgId");
        if (orgIdProperty?.CurrentValue is Guid orgId)
        {
            return orgId;
        }

        // For LeaseTerm and DepositTransaction, get OrgId from related Lease
        if (entry.Entity is LeaseTerm leaseTerm && leaseTerm.Lease != null)
        {
            return leaseTerm.Lease.OrgId;
        }

        if (entry.Entity is DepositTransaction depositTxn && depositTxn.Lease != null)
        {
            return depositTxn.Lease.OrgId;
        }

        return null;
    }

    private static string SerializeEntity(EntityEntry entry, PropertyValueType valueType)
    {
        var properties = entry.Properties
            .Where(p => !p.Metadata.Name.Equals("RowVersion", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                p => p.Metadata.Name,
                p => valueType == PropertyValueType.CurrentValue ? p.CurrentValue : p.OriginalValue
            );

        return JsonSerializer.Serialize(properties, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private enum PropertyValueType
    {
        OriginalValue,
        CurrentValue
    }
}
