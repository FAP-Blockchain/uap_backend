# JWT Token Debug Guide - StudentId & TeacherId Claims

## ?? V?n ??
API `/api/students/me/roadmap` báo l?i: **"Student ID not found in token"**

## ? Gi?i pháp ?ã th?c hi?n

### 1. **C?p nh?t UserRepository.GetByEmailAsync**
- Thêm `.Include(u => u.Student)` và `.Include(u => u.Teacher)` ?? load Student/Teacher data khi login

### 2. **C?p nh?t AuthService.GenerateJwtToken**
- Thêm **StudentId claim** n?u user là Student
- Thêm **TeacherId claim** n?u user là Teacher
- Thêm logging chi ti?t ?? debug

### 3. **C?p nh?t AuthService.LoginAsync & RefreshTokenAsync**
- ??m b?o luôn load ??y ?? Student/Teacher data tr??c khi t?o JWT token
- S? d?ng `GetByIdWithDetailsAsync` thay vì `GetByIdWithRoleAsync`

## ?? Cách ki?m tra

### B??c 1: Stop và Start l?i ?ng d?ng
```bash
# Stop ?ng d?ng ?ang ch?y (Ctrl+C ho?c Stop trong Visual Studio)
# Start l?i ?ng d?ng
```

### B??c 2: Login v?i tài kho?n Student
```http
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "email": "student1@fpt.edu.vn",
  "password": "123456"
}
```

### B??c 3: Ki?m tra logs
Trong console/output window, b?n s? th?y logs nh? sau:

```
?? ===== LOGIN DEBUG =====
?? Step 1: GetByEmailAsync returned
?? User ID: [GUID]
?? User Email: student1@fpt.edu.vn
?? User.Role: Student
?? User.Student (initial): ID=[STUDENT_GUID]
?? User.Teacher (initial): NULL
?? =========================

?? ===== JWT TOKEN GENERATION DEBUG =====
?? User ID: [GUID]
?? User Email: student1@fpt.edu.vn
?? User Role: Student
?? User.Student: ID=[STUDENT_GUID]
?? User.Teacher: NULL
? Added StudentId claim: [STUDENT_GUID]
?? No Teacher data found - TeacherId claim NOT added
?? Total claims in token: 5
?? Claim: sub = [USER_GUID]
?? Claim: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier = [USER_GUID]
?? Claim: http://schemas.microsoft.com/ws/2008/06/identity/claims/role = Student
?? Claim: email = student1@fpt.edu.vn
?? Claim: StudentId = [STUDENT_GUID]  ? ? QUAN TR?NG!
?? ========================================
```

### B??c 4: Test API `/api/students/me/roadmap`
```http
GET https://localhost:7001/api/students/me/roadmap
Authorization: Bearer [ACCESS_TOKEN_FROM_STEP_2]
```

**K?t qu? mong ??i:** API tr? v? roadmap c?a student, không còn l?i "Student ID not found in token"

## ?? Debug n?u v?n l?i

### Tr??ng h?p 1: User.Student v?n NULL sau GetByEmailAsync

**Nguyên nhân:** Student record ch?a ???c t?o ho?c UserId không kh?p

**Gi?i pháp:**
```sql
-- Ki?m tra Student records
SELECT s.*, u.Email, u.FullName 
FROM Students s
INNER JOIN Users u ON s.UserId = u.Id
WHERE u.Email = 'student1@fpt.edu.vn';

-- N?u không có, t?o Student record
INSERT INTO Students (Id, StudentCode, UserId, CreatedAt)
VALUES (NEWID(), 'SE123456', [USER_ID], GETUTCDATE());
```

### Tr??ng h?p 2: GetByIdWithDetailsAsync không load Student/Teacher

**Ki?m tra:** Xem code trong `UserRepository.GetByIdWithDetailsAsync`

```csharp
public async Task<User?> GetByIdWithDetailsAsync(Guid id)
{
    return await _dbSet
        .Include(u => u.Role)
        .Include(u => u.Student)  // ? Ph?i có dòng này
    .Include(u => u.Teacher)  // ? Ph?i có dòng này
     .FirstOrDefaultAsync(u => u.Id == id);
}
```

### Tr??ng h?p 3: StudentId claim không ???c thêm vào token

**Ki?m tra logs:** Xem dòng log "Added StudentId claim" có xu?t hi?n không

**N?u không:** User.Student = null, xem Tr??ng h?p 1

## ?? JWT Token Claims Structure

Sau khi fix, JWT token s? ch?a các claims sau:

| Claim Type | Value | Description |
|-----------|-------|-------------|
| `sub` | User.Id (GUID) | Subject - User ID |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | User.Id (GUID) | NameIdentifier |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | "Student" / "Teacher" / "Admin" | Role |
| `email` | user@example.com | Email |
| `StudentId` | Student.Id (GUID) | **NEW** - Only for Students |
| `TeacherId` | Teacher.Id (GUID) | **NEW** - Only for Teachers |

## ?? Decode JWT Token (Online Tool)

S? d?ng https://jwt.io ?? decode token và verify claims:

1. Copy Access Token t? login response
2. Paste vào jwt.io
3. Ki?m tra **Payload** section có claim `StudentId` không

**Ví d? Payload:**
```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Student",
  "email": "student1@fpt.edu.vn",
  "StudentId": "7a85f64-5717-4562-b3fc-2c963f66afa9",  ?
  "nbf": 1234567890,
  "exp": 1234571490,
  "iss": "Fap.Api",
  "aud": "Fap.Users"
}
```

## ?? K?t lu?n

Sau khi th?c hi?n các fix trên:
- ? JWT token s? ch?a StudentId claim cho Students
- ? JWT token s? ch?a TeacherId claim cho Teachers
- ? API `/api/students/me/roadmap` s? ho?t ??ng bình th??ng
- ? T?t c? API d?ng `/students/me/*` ??u có th? s? d?ng StudentId t? token

## ?? L?u ý quan tr?ng

1. **Ph?i login l?i** sau khi update code ?? nh?n token m?i có StudentId claim
2. Token c? (tr??c khi fix) s? không có StudentId claim và v?n b? l?i
3. Refresh token c?ng ?ã ???c fix ?? generate token m?i có StudentId claim
4. Có th? t?t logging debug sau khi verify xong ?? gi?m log spam

## ?? T?t Debug Logging (Optional)

Sau khi verify xong, có th? comment các dòng `_logger.LogWarning` ?? gi?m log:

```csharp
// Comment out debug logs in AuthService.cs
// _logger.LogWarning("?? ===== LOGIN DEBUG =====");
// ... other debug logs
```

Ho?c ch? log khi có l?i:
```csharp
if (user.Student == null && user.Role.Name == "Student")
{
    _logger.LogError("? Student role but no Student record found for user {UserId}", user.Id);
}
```
