# Concurrency Control, Soft Delete, and Audit Tracking Guide

## 📋 Table of Contents

1. [Overview](#overview)
2. [Optimistic Concurrency Control](#optimistic-concurrency-control)
3. [Soft Delete](#soft-delete)
4. [Automatic Audit Tracking](#automatic-audit-tracking)
5. [Implementation Guide](#implementation-guide)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Overview

This guide explains how Archu implements three critical data integrity features:

| Feature | Purpose | How It Works |
|---------|---------|--------------|
| **Optimistic Concurrency** | Prevents lost updates | SQL Server `rowversion` + EF Core concurrency tokens |
| **Soft Delete** | Preserves data history | Marks records as deleted instead of removing them |
| **Audit Tracking** | Track who changed what and when | Automatic timestamps and user tracking |

All three features are **automatic** when you inherit from `BaseEntity`.

---

## Optimistic Concurrency Control

### What is Optimistic Concurrency?

Optimistic concurrency assumes that conflicts between concurrent operations are rare. Instead of locking records, it detects conflicts when saving changes and rejects updates made to stale data.

### How It Works

```
1. Client GETs product → RowVersion: v1
2. Client edits locally
3. Client PUTs update with RowVersion: v1
4. Server checks: Is database still v1?
   ✅ YES → Save succeeds, return v2
   ❌ NO  → Return 409 Conflict
```

### Implementation

#### 1. **Domain Layer** - BaseEntity

All entities inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    
    [Timestamp]  // ← EF Core concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // IAuditable properties
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    
    // ISoftDeletable properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
```

The `[Timestamp]` attribute tells EF Core this is a concurrency token. SQL Server will automatically generate a new value on each update.

#### 2. **Database Configuration**

The `RowVersion` column is configured as SQL Server's `rowversion` type:

```sql
CREATE TABLE Products (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Price decimal(18,2) NOT NULL,
    RowVersion rowversion NOT NULL,  -- ← Auto-incremented by SQL Server
    ...
)
```

SQL Server automatically updates this value on every INSERT or UPDATE.

#### 3. **Repository Pattern** - Setting Original RowVersion

**This is the critical step** that enables concurrency detection:

```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default)
    {
        // ⚠️ CRITICAL: Set the original RowVersion to enable concurrency detection
        SetOriginalRowVersion(product, originalRowVersion);
        
        DbSet.Update(product);
        return Task.CompletedTask;
    }
}

// BaseRepository provides this method:
protected void SetOriginalRowVersion(TEntity entity, byte[] originalRowVersion)
{
    Context.Entry(entity).Property(e => e.RowVersion).OriginalValue = originalRowVersion;
}
```

This tells EF Core: *"The client was working with this specific version. If the database has a different version, throw a DbUpdateConcurrencyException."*

#### 4. **Command Handlers** - Passing RowVersion

```csharp
public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
{
    var product = await _unitOfWork.Products.GetByIdAsync(request.Id, ct);
    
    if (product is null)
        return Result<ProductDto>.Failure("Product not found");

    // Update properties
    product.Name = request.Name;
    product.Price = request.Price;

    try
    {
        // ⚠️ CRITICAL: Pass the client's RowVersion for concurrency detection
        await _unitOfWork.Products.UpdateAsync(product, request.RowVersion, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // ✅ Return the NEW RowVersion to the client
        return Result<ProductDto>.Success(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion // Updated by SQL Server
        });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Result<ProductDto>.Failure(
            "The product was modified by another user. Please refresh and try again.");
    }
}
```

#### 5. **API Layer** - Including RowVersion in DTOs

```csharp
public sealed class UpdateProductRequest
{
    [Required] public Guid Id { get; init; }
    [Required] public string Name { get; init; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    
    [Required, MinLength(1)]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();  // ← From previous GET
}

public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();  // ← For next PUT
}
```

### Client Workflow

```
1. GET /api/v1/products/123
   ← { id: 123, name: "Product", price: 100, rowVersion: "AAAAAAAA=" }

2. User edits: name = "Updated Product"

3. PUT /api/v1/products/123
   → { id: 123, name: "Updated Product", price: 100, rowVersion: "AAAAAAAA=" }

4a. Success:
    ← { id: 123, name: "Updated Product", price: 100, rowVersion: "AAAAAAAB=" }

4b. Conflict (another user updated between steps 1 and 3):
    ← 409 Conflict: "The product was modified by another user. Please refresh and try again."
```

### Why This Matters

**❌ Without setting OriginalValue:**
- Handler loads product from database (getting the latest RowVersion)
- Updates properties
- Saves changes
- **Problem**: EF Core compares the database RowVersion with itself → always matches → no conflict detection

**✅ With setting OriginalValue:**
- Handler loads product (current RowVersion: `v2`)
- Sets original RowVersion from client (client has: `v1`)
- Saves changes
- **Result**: EF Core compares `v1` (client) vs `v2` (database) → conflict detected → `DbUpdateConcurrencyException`

---

## Soft Delete

### What is Soft Delete?

Instead of physically deleting records (`DELETE FROM table`), soft delete marks records as deleted while preserving the data.

### Benefits
- Preserves audit history
- Enables "undo" functionality
- Maintains referential integrity
- Allows historical reporting

### Implementation

#### 1. **Domain Layer** - ISoftDeletable

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
```

`BaseEntity` implements this interface, so all entities support soft delete.

#### 2. **DbContext** - Global Query Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply soft delete filter to all ISoftDeletable entities
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);
            
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
```

**Effect**: All queries automatically exclude soft-deleted records. No need to add `Where(!IsDeleted)` everywhere.

#### 3. **DbContext** - Soft Delete Transform

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    ApplyAuditing();
    ApplySoftDeleteTransform(); // Transform DELETE to UPDATE
    return await base.SaveChangesAsync(ct);
}

private void ApplySoftDeleteTransform()
{
    var now = _time.UtcNow;
    var user = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                 .Where(e => e.State == EntityState.Deleted))
    {
        // Convert DELETE to UPDATE
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAtUtc = now;
        entry.Entity.DeletedBy = user;
    }
}
```

**Effect**: When you call `DbSet.Remove(entity)`, the DbContext intercepts it and converts it to an UPDATE that sets `IsDeleted = true`.

#### 4. **Repository** - DeleteAsync

```csharp
public Task DeleteAsync(Product product, CancellationToken ct = default)
{
    SoftDelete(product); // Calls DbSet.Remove(product)
    return Task.CompletedTask;
}

// BaseRepository provides:
protected void SoftDelete(TEntity entity)
{
    DbSet.Remove(entity); // Will be transformed to soft delete by DbContext
}
```

### Querying Soft-Deleted Records

To include soft-deleted records (e.g., for admin views):

```csharp
var allProducts = await _context.Products
    .IgnoreQueryFilters() // Bypasses the soft delete filter
    .ToListAsync();
```

---

## Automatic Audit Tracking

### Implementation

```csharp
private void ApplyAuditing()
{
    var now = _time.UtcNow;
    var user = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<IAuditable>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAtUtc = now;
            entry.Entity.CreatedBy = user;
        }

        if (entry.State == EntityState.Modified)
        {
            entry.Entity.ModifiedAtUtc = now;
            entry.Entity.ModifiedBy = user;
        }
    }
}
```

**Effect**: Automatically populates audit fields on every save. No manual tracking required.

### Audit Fields

| Field | Populated On | Value |
|-------|-------------|-------|
| `CreatedAtUtc` | INSERT | Current UTC time |
| `CreatedBy` | INSERT | Current user ID |
| `ModifiedAtUtc` | UPDATE | Current UTC time |
| `ModifiedBy` | UPDATE | Current user ID |
| `DeletedAtUtc` | Soft DELETE | Current UTC time |
| `DeletedBy` | Soft DELETE | Current user ID |

---

## Implementation Guide

### Quick Checklist for New Entities

- [ ] Entity inherits from `BaseEntity`
- [ ] Repository interface: `UpdateAsync(TEntity entity, byte[] originalRowVersion, ...)`
- [ ] Repository implementation: `SetOriginalRowVersion(entity, originalRowVersion)`
- [ ] DTO includes `byte[] RowVersion`
- [ ] Request includes `byte[] RowVersion` with `[Required, MinLength(1)]`
- [ ] Handler passes `request.RowVersion` to repository
- [ ] Handler returns `entity.RowVersion` after save
- [ ] Handler catches `DbUpdateConcurrencyException`

### Step-by-Step Example

See separate guide: **[Adding New Entities](../src/README_NEW_ENTITY.md)**

---

## Testing

### Manual Test: Concurrency

```bash
# 1. Create product
POST /api/v1/products
{ "name": "Test Product", "price": 100 }
→ Returns { "id": "...", "rowVersion": "AAAAAAAA=" }

# 2. Get product (User 1)
GET /api/v1/products/{id}
→ { "rowVersion": "AAAAAAAA=" }

# 3. Get product (User 2)
GET /api/v1/products/{id}
→ { "rowVersion": "AAAAAAAA=" }

# 4. Update (User 1)
PUT /api/v1/products/{id}
{ "id": "...", "name": "Updated by User 1", "price": 150, "rowVersion": "AAAAAAAA=" }
→ 200 OK: { "rowVersion": "AAAAAAAB=" }

# 5. Update (User 2 with stale RowVersion)
PUT /api/v1/products/{id}
{ "id": "...", "name": "Updated by User 2", "price": 200, "rowVersion": "AAAAAAAA=" }
→ ✅ 409 Conflict: "The product was modified by another user..."
```

### Unit Test Example

```csharp
[Fact]
public async Task UpdateProduct_WithStaleRowVersion_ReturnsConflict()
{
    // Arrange
    var product = new Product { Name = "Product A", Price = 10m };
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    
    var originalRowVersion = product.RowVersion;

    // Simulate another user updating the product
    var anotherContext = CreateNewContext();
    var sameProduct = await anotherContext.Products.FindAsync(product.Id);
    sameProduct.Price = 20m;
    await anotherContext.SaveChangesAsync(); // RowVersion changes to v2

    // Act & Assert
    var repository = new ProductRepository(_context);
    sameProduct.Price = 30m;
    
    // Use the stale RowVersion (v1) while database has v2
    await repository.UpdateAsync(sameProduct, originalRowVersion);
    
    // Should throw DbUpdateConcurrencyException
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
        _context.SaveChangesAsync());
}
```

---

## Troubleshooting

### Problem: Updates always succeed (no conflict detection)

**Solution**: Verify you're calling `SetOriginalRowVersion()` in your repository's `UpdateAsync` method.

```csharp
// ❌ Wrong
public Task UpdateAsync(Product product, byte[] originalRowVersion, ...)
{
    DbSet.Update(product);  // Missing SetOriginalRowVersion
    return Task.CompletedTask;
}

// ✅ Correct
public Task UpdateAsync(Product product, byte[] originalRowVersion, ...)
{
    SetOriginalRowVersion(product, originalRowVersion);
    DbSet.Update(product);
    return Task.CompletedTask;
}
```

### Problem: Exception "The property 'RowVersion' cannot be set"

**Solution**: Don't try to SET RowVersion directly, only set OriginalValue:

```csharp
// ❌ Wrong
product.RowVersion = request.RowVersion;

// ✅ Correct
Context.Entry(product).Property(p => p.RowVersion).OriginalValue = request.RowVersion;
```

### Problem: Client receives old RowVersion after update

**Solution**: Return `entity.RowVersion` not `request.RowVersion`:

```csharp
// ❌ Wrong
return new ProductDto { RowVersion = request.RowVersion };  // Stale!

// ✅ Correct
return new ProductDto { RowVersion = product.RowVersion };  // New version from SQL Server
```

### Problem: Soft delete creates database DELETE

**Solution**: Ensure `ApplySoftDeleteTransform()` is called in `DbContext.SaveChangesAsync()`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    ApplyAuditing();
    ApplySoftDeleteTransform();  // ← Must be called BEFORE base.SaveChangesAsync
    return await base.SaveChangesAsync(ct);
}
```

### Problem: Queries return deleted records

**Solution**: Check that global query filter is configured in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... other configuration ...
    
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            // Add query filter here
        }
    }
}
```

---

## Best Practices

1. ✅ **Always include RowVersion in DTOs** for GET and PUT operations
2. ✅ **Always pass RowVersion to UpdateAsync** in repositories
3. ✅ **Handle DbUpdateConcurrencyException** gracefully in command handlers
4. ✅ **Return user-friendly error messages** like "modified by another user"
5. ✅ **Use BaseRepository** for new entity repositories to inherit common functionality
6. ✅ **Never manually set RowVersion** - let SQL Server generate it
7. ✅ **Test concurrency scenarios** in integration tests
8. ✅ **Document expected behavior** in API documentation (Swagger)

---

## Related Files

- `src/Archu.Domain/Common/BaseEntity.cs` - Base entity with RowVersion
- `src/Archu.Infrastructure/Persistence/ApplicationDbContext.cs` - DbContext with soft delete and auditing
- `src/Archu.Infrastructure/Repositories/BaseRepository.cs` - Base repository with common functionality
- `src/Archu.Application/Products/Commands/UpdateProduct/UpdateProductCommandHandler.cs` - Example handler

---

## Further Reading

- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/modeling/concurrency)
- [SQL Server rowversion](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql)
- [Optimistic Concurrency Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/optimistic-concurrency)
- [Query Filters in EF Core](https://learn.microsoft.com/en-us/ef/core/querying/filters)

---

**Last Updated**: 2025-01-22  
**Version**: 2.0 (Consolidated)  
**Maintainer**: Archu Development Team
