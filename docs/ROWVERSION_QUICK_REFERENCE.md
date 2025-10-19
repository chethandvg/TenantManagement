# RowVersion Quick Reference

## 🔍 At a Glance

**Purpose**: Prevent lost updates in concurrent scenarios  
**Mechanism**: Two-level validation (Application + Database)  
**Technology**: SQL Server `rowversion` + EF Core concurrency tokens

---

## 📝 Quick Checklist for Updates

```csharp
// 1. Fetch entity
var product = await _unitOfWork.Products.GetByIdAsync(id, ct);
if (product is null) return NotFound();

// 2. ✅ VALIDATE RowVersion (Early)
if (!product.RowVersion.SequenceEqual(request.RowVersion))
    return Conflict("Modified by another user");

// 3. Apply changes
product.Name = request.Name;
product.Price = request.Price;

// 4. ✅ SET OriginalValue (Database validation)
await _unitOfWork.Products.UpdateAsync(product, request.RowVersion, ct);

// 5. Save and handle conflicts
try {
    await _unitOfWork.SaveChangesAsync(ct);
} catch (DbUpdateConcurrencyException) {
    return Conflict("Modified by another user");
}

// 6. ✅ RETURN new RowVersion
return Ok(new ProductDto { 
    RowVersion = product.RowVersion  // New version!
});
```

---

## ⚡ Common Mistakes

| ❌ Wrong | ✅ Correct |
|---------|----------|
| `if (product.RowVersion != request.RowVersion)` | `if (!product.RowVersion.SequenceEqual(request.RowVersion))` |
| `product.RowVersion = request.RowVersion` | `SetOriginalRowVersion(product, request.RowVersion)` |
| `return new ProductDto { RowVersion = request.RowVersion }` | `return new ProductDto { RowVersion = product.RowVersion }` |
| Skip early validation | Always validate before making changes |
| Skip database validation | Always set OriginalValue |

---

## 🎯 What Each Level Catches

### Level 1: Early Validation (SequenceEqual)
```csharp
if (!product.RowVersion.SequenceEqual(request.RowVersion))
```
**Catches**: 
- ✅ Stale client data (common case)
- ✅ Most concurrency conflicts (>95%)

**Benefits**:
- ⚡ Fast fail (no DB operations)
- 📊 Reduced load
- 💬 Clear error messages

### Level 2: Database Validation (EF Core)
```csharp
SetOriginalRowVersion(product, request.RowVersion);
await SaveChangesAsync();
```
**Catches**:
- ✅ Race conditions (rare case)
- ✅ Modifications between check and save (<5%)

**Benefits**:
- 🛡️ Guaranteed consistency
- 🔒 Database-level protection
- ✨ Handles edge cases

---

## 📊 Decision Tree

```
┌─────────────────────────┐
│  Client sends PUT with  │
│      RowVersion: v1     │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│   Fetch from database   │
│   Current version: ?    │
└───────────┬─────────────┘
            │
            ▼
     ┌──────────────┐
     │ v1 == v1 ?   │
     └──┬────────┬──┘
        │        │
     NO │        │ YES
        │        │
        ▼        ▼
   ┌─────┐   ┌──────────────────┐
   │ 409 │   │ Apply changes    │
   │     │   │ Set OriginalValue│
   └─────┘   └────────┬─────────┘
                      │
                      ▼
              ┌───────────────┐
              │ SaveChanges() │
              └───────┬───────┘
                      │
              ┌───────┴────────┐
              │                │
         Success          Race condition
              │                │
              ▼                ▼
         ┌────────┐       ┌─────┐
         │ 200 OK │       │ 409 │
         │  (v2)  │       │     │
         └────────┘       └─────┘
```

---

## 🔥 Hot Tips

1. **Always use `SequenceEqual`** for byte array comparison
2. **Never set RowVersion manually** - SQL Server generates it
3. **Return the NEW RowVersion** after successful updates
4. **Log all conflicts** for monitoring
5. **Test with concurrent requests** in integration tests

---

## 📞 Quick Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Always succeeds | Missing `SetOriginalRowVersion` | Add `SetOriginalRowVersion(entity, originalRowVersion)` |
| Always fails | Comparing with `!=` | Use `SequenceEqual` |
| Old version returned | Returning request version | Return `entity.RowVersion` |
| Race not caught | No database validation | Set OriginalValue in repository |

---

## 📚 More Information

- **Full Guide**: `CONCURRENCY_GUIDE.md`
- **Implementation Summary**: `ROWVERSION_IMPLEMENTATION_SUMMARY.md`
- **Code Examples**: See `UpdateProductCommandHandler.cs`

---

**Last Updated**: January 22, 2025  
**Quick Reference Version**: 1.0
