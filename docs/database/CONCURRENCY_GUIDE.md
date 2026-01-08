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

This guide explains how TentMan implements three critical data integrity features:

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

TentMan implements **client-side optimistic concurrency control**. This means:

1. Client GETs product and receives current RowVersion
2. Client edits the product locally
3. Client PUTs update request including the RowVersion from step 1
4. Server fetches the current product from the database
5. Server applies the changes from the request
6. Server attempts to save, using the client's RowVersion for concurrency detection
7. If the database RowVersion doesn't match the client's RowVersion, a concurrency exception is thrown

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

The handler performs **two levels of concurrency validation**:

1. **Early validation**: Explicit check before making changes (fast fail with clear error)
2. **Database validation**: EF Core check during save (handles race conditions)

```csharp
public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
{
    var product = await _unitOfWork.Products.GetByIdAsync(request.Id, ct);
    
    if (product is null)
        return Result<ProductDto>.Failure("Product not found");

    // ✅ CRITICAL: Validate RowVersion BEFORE making changes
    // This provides early detection of concurrency conflicts
    if (!product.RowVersion.SequenceEqual(request.RowVersion))
    {
        _logger.LogWarning(
            "Concurrency conflict detected for product {ProductId}. Client RowVersion does not match current database version",
            request.Id);
        
        return Result<ProductDto>.Failure(
            "The product was modified by another user. Please refresh and try again.");
    }

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
        // This handles race conditions where the product was modified
        // between our RowVersion check and SaveChangesAsync call
        if (!await _unitOfWork.Products.ExistsAsync(request.Id, ct))
            return Result<ProductDto>.Failure("Product not found");

        return Result<ProductDto>.Failure(
            "The product was modified by another user. Please refresh and try again.");
    }
}
```

**Why Two Levels of Validation?**

1. **Early Check** (`SequenceEqual`):
   - Catches most concurrency conflicts immediately
   - Provides clear logging and error messages
   - Avoids unnecessary property modifications
   - Better performance (fails fast)

2. **Database Check** (EF Core):
   - Handles race conditions (modifications between check and save)
   - Ensures database-level consistency
   - Required for true concurrency safety

**Example Scenarios:**
| Scenario | Early Check | DB Check | Result |
|----------|-------------|----------|--------|
| No conflict | ✅ Pass | ✅ Pass | Success |
| Stale client data | ❌ Fail | N/A | 409 Conflict (early) |
| Race condition | ✅ Pass | ❌ Fail | 409 Conflict (at save) |
 
#### 5. **API Layer** - Including RowVersion in DTOs

```csharp
public sealed class UpdateProductRequest : IValidatableObject
{
    [Required] 
    public Guid Id { get; init; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;
    
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; init; }
    
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

**Command Definition:**

```csharp
public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    byte[] RowVersion  // ← From client
) : IRequest<Result<ProductDto>>;
```

**Controller Implementation:**

```csharp
var command = new UpdateProductCommand(
    request.Id, 
    request.Name, 
    request.Price, 
    request.RowVersion);  // ← Pass from client

var result = await _mediator.Send(command, cancellationToken);
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

**Two-Level Concurrency Protection:**

**Level 1: Early Validation (Application Layer)**
```csharp
if (!product.RowVersion.SequenceEqual(request.RowVersion))
{
    return Result<ProductDto>.Failure("The product was modified by another user...");
}
```
- Catches most concurrency conflicts immediately
- Fails fast before modifying entity properties
- Provides clear, specific error messages
- Better performance (no database round-trip for failed updates)

**Level 2: Database Validation (EF Core + SQL Server)**
```csharp
Context.Entry(product).Property(e => e.RowVersion).OriginalValue = request.RowVersion;
await SaveChangesAsync();  // SQL Server checks RowVersion
```
- Handles race conditions (modifications between Level 1 check and save)
- Ensures database-level consistency
- Required for true concurrency safety
- Throws `DbUpdateConcurrencyException` if conflict detected

**Example Timeline:**
```
Time    User 1                           User 2                      Database
----    ------                           ------                      --------
t0      GET product (v1)                                            v1
t1                                       GET product (v1)            v1
t2      Modify locally
t3                                       Modify locally
t4      PUT with v1
        → Fetch from DB (v1)
        → Early check: v1 == v1 ✅
        → Update properties
        → Set OriginalValue = v1
        → Save succeeds
        → Return v2                                                  v2 ✅
t5                                       PUT with v1
                                         → Fetch from DB (v2)
                                         → Early check: v2 != v1 ❌
                                         → Return 409 Conflict
```

**❌ Without Two-Level Protection:**
- Without early check: Unnecessary property modifications and logging
- Without EF Core check: Race conditions can cause data loss
- **Problem**: Data integrity at risk

**✅ With Two-Level Protection:**
- Early check catches most conflicts immediately
- EF Core check handles race conditions
- **Result**: Maximum safety + best performance

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
- [ ] Command includes `byte[] RowVersion` parameter
- [ ] Controller passes `request.RowVersion` to command
- [ ] **Handler validates RowVersion with `SequenceEqual` before making changes** ✨
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
→ ✅ 409 Conflict: "The product was modified by another user. Please refresh and try again."
```

### Unit Test Examples

#### Test 1: Early Validation Catches Stale RowVersion

```csharp
[Fact]
public async Task UpdateProduct_WithStaleRowVersion_ReturnsConflictFromEarlyValidation()
{
    // Arrange
    var product = new Product { Name = "Product A", Price = 10m };
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    
    var staleRowVersion = product.RowVersion;

    // Simulate another user updating the product
    product.Price = 20m;
    await _context.SaveChangesAsync(); // RowVersion changes to v2

    // Act - Try to update with stale RowVersion
    var command = new UpdateProductCommand(
        product.Id, 
        "Updated Name", 
        30m, 
        staleRowVersion); // Using v1, but database has v2
    
    var handler = new UpdateProductCommandHandler(_unitOfWork, _logger);
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("The product was modified by another user. Please refresh and try again.", 
        result.Error);
}
```

#### Test 2: Database Validation Catches Race Condition

```csharp
[Fact]
public async Task UpdateProduct_WithRaceCondition_ReturnsConflictFromDatabaseValidation()
{
    // Arrange
    var product = new Product { Name = "Product A", Price = 10m };
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    
    var originalRowVersion = product.RowVersion;

    // Act & Assert
    // Simulate race condition: Another context updates between fetch and save
    var anotherContext = CreateNewContext();
    var sameProduct = await anotherContext.Products.FindAsync(product.Id);
    
    // User 1 starts update (early validation passes)
    var repository = new ProductRepository(_context);
    var productToUpdate = await _context.Products.FindAsync(product.Id);
    var capturedRowVersion = productToUpdate!.RowVersion;
    
    // User 2 completes update (race condition)
    sameProduct!.Price = 20m;
    await anotherContext.SaveChangesAsync(); // RowVersion changes to v2
    
    // User 1 continues (early check passed, but database check will fail)
    productToUpdate.Price = 30m;
    await repository.UpdateAsync(productToUpdate, capturedRowVersion);
    
    // Should throw DbUpdateConcurrencyException because database has v2
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
        _context.SaveChangesAsync());
}
```

#### Test 3: Successful Update Returns New RowVersion

```csharp
[Fact]
public async Task UpdateProduct_WithValidRowVersion_ReturnsSuccessWithNewRowVersion()
{
    // Arrange
    var product = new Product { Name = "Product A", Price = 10m };
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    
    var originalRowVersion = product.RowVersion;

    // Act
    var command = new UpdateProductCommand(
        product.Id, 
        "Updated Name", 
        20m, 
        originalRowVersion);
    
    var handler = new UpdateProductCommandHandler(_unitOfWork, _logger);
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Updated Name", result.Value!.Name);
    Assert.Equal(20m, result.Value.Price);
    Assert.NotEqual(originalRowVersion, result.Value.RowVersion); // New version
}
```

#### Test 4: SequenceEqual Works Correctly for Byte Arrays

```csharp
[Fact]
public void RowVersion_SequenceEqual_WorksCorrectly()
{
    // Arrange
    byte[] version1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
    byte[] version1Copy = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
    byte[] version2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };

    // Assert
    Assert.True(version1.SequenceEqual(version1Copy));  // Same content
    Assert.False(version1.SequenceEqual(version2));     // Different content
    Assert.False(version1 == version1Copy);             // Different references
}
```

### Integration Test: Full Concurrency Scenario

```csharp
[Fact]
public async Task IntegrationTest_ConcurrentUpdates_SecondUpdateFailsWithConflict()
{
    // Arrange - Create a product
    var createResponse = await _client.PostAsJsonAsync("/api/v1/products", 
        new { name = "Test Product", price = 100 });
    var createdProduct = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
    var productId = createdProduct!.Data!.Id;

    // User 1 and User 2 both GET the product
    var user1Response = await _client.GetAsync($"/api/v1/products/{productId}");
    var user1Product = await user1Response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
    
    var user2Response = await _client.GetAsync($"/api/v1/products/{productId}");
    var user2Product = await user2Response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();

    // Both users have the same RowVersion
    Assert.Equal(user1Product!.Data!.RowVersion, user2Product!.Data!.RowVersion);

    // User 1 updates successfully
    var user1Update = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", 
        new 
        { 
            id = productId,
            name = "Updated by User 1", 
            price = 150,
            rowVersion = user1Product.Data.RowVersion 
        });
    
    Assert.Equal(HttpStatusCode.OK, user1Update.StatusCode);

    // User 2 tries to update with stale RowVersion
    var user2Update = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", 
        new 
        { 
            id = productId,
            name = "Updated by User 2", 
            price = 200,
            rowVersion = user2Product.Data.RowVersion // Stale version
        });
    
    // Assert - User 2 gets conflict
    Assert.Equal(HttpStatusCode.Conflict, user2Update.StatusCode);
    var errorResponse = await user2Update.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
    Assert.Contains("modified by another user", errorResponse!.Message!.ToLower());
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

### Problem: RowVersion validation always fails with SequenceEqual

**Symptoms:**
- Early validation fails even when client has correct RowVersion
- Error: "The product was modified by another user" immediately after GET

**Solution**: Check that you're comparing byte arrays correctly and that RowVersion is being returned properly in DTOs.

```csharp
// ❌ Wrong - comparing references
if (product.RowVersion != request.RowVersion)  // Always false for byte arrays

// ✅ Correct - comparing contents
if (!product.RowVersion.SequenceEqual(request.RowVersion))

// Also verify DTO mapping includes RowVersion
return new ProductDto
{
    Id = product.Id,
    Name = product.Name,
    Price = product.Price,
    RowVersion = product.RowVersion  // ← Must be included
};
```

### Problem: Early validation passes but database validation fails

**Symptoms:**
- `SequenceEqual` check passes
- `DbUpdateConcurrencyException` thrown during save
- This is **expected behavior** for race conditions

**Explanation**: This is normal and correct! It means:
1. Your RowVersion was valid when you checked it
2. Another user modified the product between your check and save
3. EF Core caught the race condition

**Solution**: This is working as designed. Ensure you're catching and handling `DbUpdateConcurrencyException`:

```csharp
catch (DbUpdateConcurrencyException)
{
    _logger.LogWarning("Race condition detected: Product modified between validation and save");
    return Result<ProductDto>.Failure(
        "The product was modified by another user. Please refresh and try again.");
}
```

### Problem: Exception "The property 'RowVersion' cannot be set"

**Solution**: Don't try to SET RowVersion directly, only set OriginalValue:

```csharp
// ❌ Wrong
product.RowVersion = request.RowVersion;

// ✅ Correct
Context.Entry(product).Property(e => e.RowVersion).OriginalValue = request.RowVersion;
```

### Problem: Client receives old RowVersion after update

**Solution**: Return `entity.RowVersion` not `request.RowVersion`:

```csharp
// ❌ Wrong
return new ProductDto { RowVersion = request.RowVersion };  // Stale!

// ✅ Correct
return new ProductDto { RowVersion = product.RowVersion };  // New version from SQL Server
```

### Problem: RowVersion is null or empty in response

**Symptoms:**
- GET request returns empty byte array for RowVersion
- Or RowVersion is null

**Solution**: Verify the database column is configured correctly and entity is being tracked:

```csharp
// Check entity configuration
modelBuilder.Entity<Product>(entity =>
{
    entity.Property(e => e.RowVersion)
        .IsRowVersion()  // ← Critical for SQL Server
        .IsRequired();
});

// Check query is tracking the entity (for updates)
var product = await DbSet
    .FirstOrDefaultAsync(p => p.Id == id, ct);  // ✅ Tracked

// Not this (for updates):
var product = await DbSet
    .AsNoTracking()  // ❌ Not tracked - RowVersion won't update
    .FirstOrDefaultAsync(p => p.Id == id, ct);
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
2. ✅ **Validate RowVersion explicitly** in handlers before making changes (use `SequenceEqual`)
3. ✅ **Always pass RowVersion to UpdateAsync** in repositories (sets `OriginalValue`)
4. ✅ **Handle DbUpdateConcurrencyException** gracefully in command handlers (for race conditions)
5. ✅ **Return user-friendly error messages** like "modified by another user"
6. ✅ **Use BaseRepository** for new entity repositories to inherit common functionality
7. ✅ **Never manually set RowVersion** - let SQL Server generate it
8. ✅ **Return the NEW RowVersion** after successful updates (not the request's RowVersion)
9. ✅ **Log concurrency conflicts** for monitoring and debugging
10. ✅ **Test concurrency scenarios** in integration tests
11. ✅ **Document expected behavior** in API documentation (Swagger)
12. ✅ **Use two-level validation** (early check + database check) for best safety and performance

### Performance Considerations

- **Early validation** (`SequenceEqual`) is very fast - O(n) byte array comparison
- Prevents unnecessary property modifications when conflict is certain
- Reduces database load by failing fast
- The rare race condition case still requires database validation

### Security Considerations

- RowVersion prevents **lost update attacks** (overwrites by malicious users)
- Client must provide exact version they're modifying
- Server validates both in-memory and at database level
- Audit logs capture all concurrency conflicts

---

## Related Files

- `src/TentMan.Domain/Common/BaseEntity.cs` - Base entity with RowVersion
- `src/TentMan.Infrastructure/Persistence/ApplicationDbContext.cs` - DbContext with soft delete and auditing
- `src/TentMan.Infrastructure/Repositories/BaseRepository.cs` - Base repository with common functionality
- `src/TentMan.Application/Products/Commands/UpdateProduct/UpdateProductCommandHandler.cs` - Example handler
- `src/TentMan.Contracts/Products/UpdateProductRequest.cs` - API request contract with RowVersion
- `src/TentMan.Api/Controllers/ProductsController.cs` - API controller

---

## Further Reading

- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/modeling/concurrency)
- [SQL Server rowversion](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql)
- [Optimistic Concurrency Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/optimistic-concurrency)
- [Query Filters in EF Core](https://learn.microsoft.com/en-us/ef/core/querying/filters)

---

**Last Updated**: 2025-01-22  
**Version**: 2.0 (Consolidated - Client-Side Concurrency Control)  
**Maintainer**: TentMan Development Team
