# Security Restrictions Quick Reference

## 🎯 TL;DR - What's Implemented

### ✅ **Role Assignment Rules**
- **SuperAdmin** → Can assign ANY role
- **Administrator** → Can assign User, Manager, Guest ONLY (NOT SuperAdmin/Administrator)
- **Manager** → Cannot assign roles

### ✅ **Role Removal Rules**
- **SuperAdmin** → Can remove any role (except own SuperAdmin, last SuperAdmin)
- **Administrator** → Can remove User, Manager, Guest ONLY
- **Cannot** remove your own privileged roles (SuperAdmin/Administrator)
- **Cannot** remove last SuperAdmin from system

### ✅ **User Deletion Rules**
- **Cannot** delete yourself
- **Cannot** delete last SuperAdmin in system
- **SuperAdmin & Administrator** can delete users

---

## 📊 Permission Matrix

### Role Assignment

| Admin Role | Can Assign → | SuperAdmin | Administrator | Manager | User | Guest |
|-----------|--------------|-----------|--------------|---------|------|-------|
| **SuperAdmin** | | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Administrator** | | ❌ | ❌ | ✅ | ✅ | ✅ |
| **Manager** | | ❌ | ❌ | ❌ | ❌ | ❌ |

### Role Removal

| Admin Role | Can Remove → | SuperAdmin | Administrator | Manager | User | Guest |
|-----------|--------------|-----------|--------------|---------|------|-------|
| **SuperAdmin** | | ✅* | ✅ | ✅ | ✅ | ✅ |
| **Administrator** | | ❌ | ❌** | ✅ | ✅ | ✅ |
| **Manager** | | ❌ | ❌ | ❌ | ❌ | ❌ |

*Except own SuperAdmin or last SuperAdmin  
**Except own Administrator

### User Deletion

| Admin Role | Can Delete → | Any User | Last SuperAdmin | Self |
|-----------|--------------|----------|-----------------|------|
| **SuperAdmin** | | ✅ | ❌ | ❌ |
| **Administrator** | | ✅ | ❌ | ❌ |
| **Manager** | | ❌ | ❌ | ❌ |

---

## 🔥 Common Error Messages

### "Permission denied: Only SuperAdmin can assign the 'SuperAdmin' role"
**Why:** Administrator tried to assign SuperAdmin role  
**Who Can Fix:** Only SuperAdmin can assign SuperAdmin role  
**Solution:** Have a SuperAdmin assign the role

### "Security restriction: You cannot remove your own SuperAdmin role"
**Why:** SuperAdmin tried to remove their own SuperAdmin role  
**Who Can Fix:** Another SuperAdmin  
**Solution:** Have another SuperAdmin remove the role

### "Critical security restriction: Cannot remove the last SuperAdmin role from the system"
**Why:** Attempting to remove SuperAdmin role from the only SuperAdmin  
**Who Can Fix:** N/A - System protection  
**Solution:** Create another SuperAdmin first, then remove

### "Security restriction: You cannot delete your own account"
**Why:** Admin tried to delete their own account  
**Who Can Fix:** Another admin  
**Solution:** Have another admin delete the account

### "Critical security restriction: Cannot delete the last SuperAdmin user from the system"
**Why:** Attempting to delete the only SuperAdmin  
**Who Can Fix:** N/A - System protection  
**Solution:** Create another SuperAdmin first, then delete

---

## 🎬 Quick Start Testing

### 1. Test Role Assignment Restriction
```bash
# As Administrator, try to assign SuperAdmin (should fail)
curl -X POST https://localhost:5001/api/v1/admin/user-roles/assign \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "target-user-id",
    "roleId": "superadmin-role-id"
  }'

# Expected: 400 Bad Request with permission denied message
```

### 2. Test Self-Removal Prevention
```bash
# Try to remove your own SuperAdmin role (should fail)
curl -X DELETE https://localhost:5001/api/v1/admin/user-roles/{your-id}/roles/{superadmin-role-id} \
  -H "Authorization: Bearer <your-superadmin-token>"

# Expected: 400 Bad Request with self-removal restriction message
```

### 3. Test Last SuperAdmin Protection
```bash
# Try to delete the only SuperAdmin (should fail)
curl -X DELETE https://localhost:5001/api/v1/admin/users/{only-superadmin-id} \
  -H "Authorization: Bearer <superadmin-token>"

# Expected: 400 Bad Request with last SuperAdmin protection message
```

---

## 🛠️ Files Modified/Created

### New Files:
- `src/Archu.Application/Admin/Commands/RemoveRole/RemoveRoleCommand.cs`
- `src/Archu.Application/Admin/Commands/RemoveRole/RemoveRoleCommandHandler.cs`
- `src/Archu.Application/Admin/Commands/DeleteUser/DeleteUserCommand.cs`
- `src/Archu.Application/Admin/Commands/DeleteUser/DeleteUserCommandHandler.cs`

### Modified Files:
- `src/Archu.Application/Admin/Commands/AssignRole/AssignRoleCommandHandler.cs`
- `Archu.AdminApi/Controllers/UserRolesController.cs`
- `Archu.AdminApi/Controllers/UsersController.cs`

---

## 🔍 Troubleshooting

### Issue: "Administrator can't assign Manager role"
**Check:** Is the user actually an Administrator? Use `GET /api/v1/admin/user-roles/{userId}` to verify

### Issue: "Getting 403 Forbidden"
**Check:** Authorization policy might be blocking. Ensure user has Admin access policy

### Issue: "Can't remove role that user doesn't have"
**Check:** User must have the role. Use `GET /api/v1/admin/user-roles/{userId}` to see current roles

### Issue: "Logs show 'Unauthorized role assignment attempt'"
**Check:** User might not be SuperAdmin or Administrator. Check user's roles in JWT token

---

## 📞 Support

For questions or issues:
1. Check logs in Admin API output
2. Review [Full Documentation](ROLE_ASSIGNMENT_REMOVAL_RESTRICTIONS.md)
3. Check [Authorization Guide](ADMIN_API_AUTHORIZATION_GUIDE.md)

---

**Version:** 1.0  
**Last Updated:** 2025-01-22
