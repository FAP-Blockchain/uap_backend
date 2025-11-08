# ?? API Endpoints Documentation

## Subjects API Endpoints

### 1. GET /api/subjects
**Mô t?:** L?y danh sách môn h?c v?i phân trang, filter và sort

**Authorization:** Authenticated users

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `SearchTerm` (string, optional) - Tìm ki?m theo mã ho?c tên môn h?c
- `SemesterId` (guid, optional) - L?c theo h?c k?
- `SortBy` (string, default: "SubjectCode") - SubjectCode, SubjectName, Credits
- `IsDescending` (bool, default: false)

**Response:**
```json
{
  "data": [
    {
      "id": "guid",
      "subjectCode": "string",
      "subjectName": "string",
      "credits": 0,
 "semesterId": "guid",
      "semesterName": "string",
      "totalClasses": 0
}
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

---

### 2. GET /api/subjects/{id}
**Mô t?:** L?y thông tin chi ti?t môn h?c theo ID

**Authorization:** Authenticated users

**Response:**
```json
{
  "id": "guid",
  "subjectCode": "string",
  "subjectName": "string",
  "credits": 0,
  "semesterId": "guid",
  "semesterName": "string",
  "semesterStartDate": "datetime",
  "semesterEndDate": "datetime",
  "classes": [
    {
      "id": "guid",
"classCode": "string",
      "teacherName": "string",
      "currentEnrollment": 0,
      "maxEnrollment": 0
}
  ],
"totalStudentsEnrolled": 0
}
```

---

### 3. POST /api/subjects
**Mô t?:** T?o môn h?c m?i

**Authorization:** Admin only

**Request Body:**
```json
{
  "subjectCode": "string (required, max 50)",
  "subjectName": "string (required, max 150)",
  "credits": "int (1-10)",
  "semesterId": "guid (required)"
}
```

**Response:**
```json
{
  "message": "Subject created successfully",
  "subjectId": "guid"
}
```

**Business Rules:**
- Subject code ph?i unique
- Semester ph?i t?n t?i
- Không th? thêm môn h?c vào h?c k? ?ã ?óng (IsClosed = true)

---

### 4. PUT /api/subjects/{id}
**Mô t?:** C?p nh?t thông tin môn h?c

**Authorization:** Admin only

**Request Body:**
```json
{
  "subjectCode": "string (required, max 50)",
  "subjectName": "string (required, max 150)",
  "credits": "int (1-10)",
  "semesterId": "guid (required)"
}
```

**Business Rules:**
- Subject code không ???c trùng v?i môn h?c khác
- Semester ph?i t?n t?i
- Không th? c?p nh?t môn h?c trong h?c k? ?ã ?óng

---

### 5. DELETE /api/subjects/{id}
**Mô t?:** Xóa môn h?c

**Authorization:** Admin only

**Business Rules:**
- Không th? xóa môn h?c ?ã có l?p h?c
- Không th? xóa môn h?c trong h?c k? ?ã ?óng

---

## Semesters API Endpoints

### 1. GET /api/semesters
**Mô t?:** L?y danh sách h?c k? v?i phân trang, filter và sort

**Authorization:** Authenticated users

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `SearchTerm` (string, optional) - Tìm ki?m theo tên h?c k?
- `IsActive` (bool, optional) - L?c h?c k? ?ang ho?t ??ng
- `IsClosed` (bool, optional) - L?c h?c k? ?ã ?óng
- `SortBy` (string, default: "StartDate") - Name, StartDate, EndDate
- `IsDescending` (bool, default: true)

**Response:**
```json
{
  "data": [
  {
      "id": "guid",
      "name": "string",
      "startDate": "datetime",
      "endDate": "datetime",
      "totalSubjects": 0,
      "isActive": false,
      "isClosed": false
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

---

### 2. GET /api/semesters/{id}
**Mô t?:** L?y thông tin chi ti?t h?c k? theo ID

**Authorization:** Authenticated users

**Response:**
```json
{
  "id": "guid",
  "name": "string",
  "startDate": "datetime",
  "endDate": "datetime",
  "isActive": false,
  "isClosed": false,
  "totalSubjects": 0,
  "totalClasses": 0,
  "totalStudentsEnrolled": 0,
  "subjects": [
    {
   "id": "guid",
      "subjectCode": "string",
      "subjectName": "string",
      "credits": 0,
      "totalClasses": 0
    }
  ]
}
```

---

### 3. POST /api/semesters
**Mô t?:** T?o h?c k? m?i

**Authorization:** Admin only

**Request Body:**
```json
{
  "name": "string (required, max 80)",
  "startDate": "datetime (required)",
  "endDate": "datetime (required)"
}
```

**Response:**
```json
{
  "message": "Semester created successfully",
  "semesterId": "guid"
}
```

**Business Rules:**
- StartDate ph?i tr??c EndDate
- EndDate ph?i ? t??ng lai
- Tên h?c k? ph?i unique
- Không ???c trùng th?i gian v?i h?c k? khác

---

### 4. PUT /api/semesters/{id}
**Mô t?:** C?p nh?t thông tin h?c k?

**Authorization:** Admin only

**Request Body:**
```json
{
  "name": "string (required, max 80)",
  "startDate": "datetime (required)",
  "endDate": "datetime (required)"
}
```

**Business Rules:**
- Không th? c?p nh?t h?c k? ?ã ?óng
- StartDate ph?i tr??c EndDate
- Tên không ???c trùng v?i h?c k? khác
- Th?i gian không ???c trùng v?i h?c k? khác

---

### 5. PATCH /api/semesters/{id}/close
**Mô t?:** ?óng h?c k? (ng?n ch?n các thay ??i trong t??ng lai)

**Authorization:** Admin only

**Business Rules:**
- H?c k? ph?i ?ã k?t thúc (EndDate < now)
- Không th? ?óng h?c k? ?ã ?óng

**Effects:**
- Khi h?c k? ?ã ?óng:
  - Không th? thêm môn h?c m?i
  - Không th? c?p nh?t môn h?c
  - Không th? xóa môn h?c
  - Không th? c?p nh?t thông tin h?c k?

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Error message describing the issue"
}
```

### 401 Unauthorized
Khi user ch?a ??ng nh?p

### 403 Forbidden
Khi user không có quy?n Admin

### 404 Not Found
```json
{
  "message": "Subject/Semester with ID {id} not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while processing request"
}
```

---

## Migration Command

?? áp d?ng migration cho database:

```bash
dotnet ef database update --project Fap.Infrastructure --startup-project Fap.Api
```

---

## Testing v?i Swagger

1. Ch?y ?ng d?ng
2. Truy c?p: `https://localhost:{port}/swagger`
3. ??ng nh?p ?? l?y JWT token
4. Click "Authorize" và nh?p: `Bearer {your-token}`
5. Test các endpoints

---

## Architecture Overview

```
Controllers (API Layer)
    ?
Services (Business Logic)
    ?
Unit of Work + Repositories
    ?
DbContext (Data Access)
```

**Design Patterns Used:**
- ? Repository Pattern
- ? Unit of Work Pattern
- ? Dependency Injection
- ? DTO Pattern
- ? Service Layer Pattern
- ? Options Pattern

---

## Notes

- T?t c? dates ??u s? d?ng UTC
- Pagination default: PageNumber=1, PageSize=10
- Sorting m?c ??nh: Subjects by SubjectCode, Semesters by StartDate (desc)
- Authorization s? d?ng JWT Bearer token
- Admin role required cho CREATE/UPDATE/DELETE operations
