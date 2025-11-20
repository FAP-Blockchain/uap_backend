# Scripts Folder

## ?? Các Script có s?n

### ?? Database Management

#### `reset-azure-database.ps1`
Script chính ?? qu?n lý database trên Azure qua API.

**Cách dùng:**
```powershell
.\reset-azure-database.ps1 -ApiUrl "https://your-app.azurewebsites.net" -Action "reset"
```

**Actions:**
- `reset` - Reset toàn b? database
- `migrate` - Apply migrations
- `reseed` - Seed l?i data
- `status` - Ki?m tra tr?ng thái

---

#### `quick-reset-menu.ps1`
Menu interactive ?? d? dàng s? d?ng.

**Cách dùng:**
```powershell
.\quick-reset-menu.ps1
```

Sau ?ó ch?n option t? menu.

?? **C?u hình**: S?a bi?n `$AZURE_API_URL` trong file.

---

### ?? Documentation

Xem file `Docs/RESET_AZURE_DATABASE.md` ?? bi?t chi ti?t.
