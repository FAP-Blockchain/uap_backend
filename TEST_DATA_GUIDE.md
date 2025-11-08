# ?? Test Data Guide - Subjects & Semesters API

## ?? Seeded Data Overview

### ?? Users (7 total)

| Email | Password | Role | Full Name |
|-------|----------|------|-----------|
| admin@fap.edu.vn | 123456 | Admin | Administrator |
| teacher1@fap.edu.vn | 123456 | Teacher | Nguy?n V?n Giáo Viên |
| teacher2@fap.edu.vn | 123456 | Teacher | Tr?n Th? H?ng |
| teacher3@fap.edu.vn | 123456 | Teacher | Lê V?n Toán |
| student1@fap.edu.vn | 123456 | Student | Nguy?n V?n An (SV001) |
| student2@fap.edu.vn | 123456 | Student | Ph?m Th? Bình (SV002) |
| student3@fap.edu.vn | 123456 | Student | Hoàng V?n Châu (SV003) |

---

### ?? Semesters (5 total)

| ID | Name | Start Date | End Date | Status | Subjects | Classes |
|----|------|------------|----------|--------|----------|---------|
| d1111111... | Spring 2024 | 2024-01-15 | 2024-05-15 | ? **Closed** | 2 | 2 |
| d2222222... | Summer 2024 | 2024-06-01 | 2024-08-31 | Active | 2 | 3 |
| d3333333... | Fall 2024 | 2024-09-01 | 2024-12-31 | Active | 3 | 2 |
| d4444444... | Spring 2025 | 2025-01-15 | 2025-05-15 | Future | 2 | 0 |
| d5555555... | Fall 2025 | 2025-09-01 | 2025-12-31 | Future | 1 | 0 |

---

### ?? Subjects (10 total)

#### Spring 2024 (Closed Semester) ?
| Code | Name | Credits | Classes | Students |
|------|------|---------|---------|----------|
| BC101 | Blockchain Fundamentals | 3 | 1 (BC101-A) | 3 |
| SE201 | Software Engineering | 4 | 1 (SE201-A) | 2 |

#### Summer 2024
| Code | Name | Credits | Classes | Students |
|------|------|---------|---------|----------|
| DB301 | Database Management Systems | 3 | 1 (DB301-A) | 3 |
| BC202 | Smart Contract Development | 4 | 2 (BC202-A, BC202-B) | 3 |

#### Fall 2024
| Code | Name | Credits | Classes | Students |
|------|------|---------|---------|----------|
| BC303 | Blockchain Security | 3 | 1 (BC303-A) | 2 |
| AI401 | Artificial Intelligence | 4 | 1 (AI401-A) | 0 |
| NW201 | Computer Networks | 3 | 0 | 0 |

#### Spring 2025
| Code | Name | Credits | Classes | Students |
|------|------|---------|---------|----------|
| DS501 | Data Structures & Algorithms | 4 | 0 | 0 |
| WEB301 | Web Development | 3 | 0 | 0 |

#### Fall 2025
| Code | Name | Credits | Classes | Students |
|------|------|---------|---------|----------|
| ML501 | Machine Learning | 4 | 0 | 0 |

---

### ?? Classes (7 total)

| Class Code | Subject | Teacher | Students | Semester |
|------------|---------|---------|----------|----------|
| BC101-A | Blockchain Fundamentals | GV001 | 3 | Spring 2024 ? |
| SE201-A | Software Engineering | GV002 | 2 | Spring 2024 ? |
| DB301-A | Database Management Systems | GV003 | 3 | Summer 2024 |
| BC202-A | Smart Contract Development | GV001 | 2 | Summer 2024 |
| BC202-B | Smart Contract Development | GV001 | 1 | Summer 2024 |
| BC303-A | Blockchain Security | GV001 | 2 | Fall 2024 |
| AI401-A | Artificial Intelligence | GV002 | 0 | Fall 2024 |

---

## ?? Test Scenarios

### **Semesters API Testing**

#### ? **Test Case 1: List All Semesters**
```http
GET /api/semesters?PageNumber=1&PageSize=10
Authorization: Bearer {token}
```
**Expected:** 5 semesters, sorted by StartDate descending

---

#### ? **Test Case 2: Filter Active Semesters**
```http
GET /api/semesters?IsActive=true
Authorization: Bearer {token}
```
**Expected:** Summer 2024, Fall 2024 (depending on current date)

---

#### ? **Test Case 3: Filter Closed Semesters**
```http
GET /api/semesters?IsClosed=true
Authorization: Bearer {token}
```
**Expected:** Spring 2024

---

#### ? **Test Case 4: Search Semester by Name**
```http
GET /api/semesters?SearchTerm=2024
Authorization: Bearer {token}
```
**Expected:** Spring 2024, Summer 2024, Fall 2024

---

#### ? **Test Case 5: Get Semester Details**
```http
GET /api/semesters/d1111111-1111-1111-1111-111111111111
Authorization: Bearer {token}
```
**Expected:** 
- Spring 2024 details
- 2 subjects (BC101, SE201)
- 2 classes
- 5 total students enrolled
- IsClosed = true

---

#### ? **Test Case 6: Create New Semester (Admin)**
```http
POST /api/semesters
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "name": "Winter 2025",
  "startDate": "2025-12-15T00:00:00Z",
  "endDate": "2026-02-28T00:00:00Z"
}
```
**Expected:** 201 Created

---

#### ? **Test Case 7: Create Semester with Overlapping Dates (Should Fail)**
```http
POST /api/semesters
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "name": "Test Overlap",
  "startDate": "2024-01-20T00:00:00Z",
  "endDate": "2024-05-10T00:00:00Z"
}
```
**Expected:** 400 Bad Request - "The date range overlaps with an existing semester"

---

#### ? **Test Case 8: Update Closed Semester (Should Fail)**
```http
PUT /api/semesters/d1111111-1111-1111-1111-111111111111
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "name": "Spring 2024 Updated",
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2024-05-15T00:00:00Z"
}
```
**Expected:** 400 Bad Request - "Cannot update a closed semester"

---

#### ? **Test Case 9: Close Semester (Admin)**
```http
PATCH /api/semesters/d2222222-2222-2222-2222-222222222222/close
Authorization: Bearer {admin-token}
```
**Expected:** 200 OK (if Summer 2024 has ended)

---

### **Subjects API Testing**

#### ? **Test Case 10: List All Subjects**
```http
GET /api/subjects?PageNumber=1&PageSize=10
Authorization: Bearer {token}
```
**Expected:** 10 subjects

---

#### ? **Test Case 11: Filter Subjects by Semester**
```http
GET /api/subjects?SemesterId=d3333333-3333-3333-3333-333333333333
Authorization: Bearer {token}
```
**Expected:** 3 subjects (BC303, AI401, NW201) from Fall 2024

---

#### ? **Test Case 12: Search Subjects**
```http
GET /api/subjects?SearchTerm=blockchain
Authorization: Bearer {token}
```
**Expected:** BC101, BC202, BC303

---

#### ? **Test Case 13: Sort Subjects by Credits**
```http
GET /api/subjects?SortBy=Credits&IsDescending=true
Authorization: Bearer {token}
```
**Expected:** 4-credit subjects first (SE201, BC202, AI401, DS501, ML501)

---

#### ? **Test Case 14: Get Subject Details**
```http
GET /api/subjects/e1111111-1111-1111-1111-111111111111
Authorization: Bearer {token}
```
**Expected:** 
- BC101 details
- 1 class (BC101-A)
- Teacher: GV001
- 3 students enrolled

---

#### ? **Test Case 15: Create New Subject (Admin)**
```http
POST /api/subjects
Authorization: Bearer {admin-token}
Content-Type: application/json

{
"subjectCode": "TEST101",
  "subjectName": "Test Subject",
  "credits": 3,
  "semesterId": "d4444444-4444-4444-4444-444444444444"
}
```
**Expected:** 201 Created

---

#### ? **Test Case 16: Create Subject with Duplicate Code (Should Fail)**
```http
POST /api/subjects
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "subjectCode": "BC101",
  "subjectName": "Duplicate Test",
  "credits": 3,
  "semesterId": "d4444444-4444-4444-4444-444444444444"
}
```
**Expected:** 400 Bad Request - "Subject with code 'BC101' already exists"

---

#### ? **Test Case 17: Create Subject in Closed Semester (Should Fail)**
```http
POST /api/subjects
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "subjectCode": "NEW101",
  "subjectName": "New Subject",
  "credits": 3,
  "semesterId": "d1111111-1111-1111-1111-111111111111"
}
```
**Expected:** 400 Bad Request - "Cannot add subjects to a closed semester"

---

#### ? **Test Case 18: Update Subject**
```http
PUT /api/subjects/e8888888-8888-8888-8888-888888888888
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "subjectCode": "DS501",
  "subjectName": "Data Structures & Algorithms - Updated",
  "credits": 4,
  "semesterId": "d4444444-4444-4444-4444-444444444444"
}
```
**Expected:** 200 OK

---

#### ? **Test Case 19: Update Subject in Closed Semester (Should Fail)**
```http
PUT /api/subjects/e1111111-1111-1111-1111-111111111111
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "subjectCode": "BC101",
  "subjectName": "Updated Name",
  "credits": 3,
  "semesterId": "d1111111-1111-1111-1111-111111111111"
}
```
**Expected:** 400 Bad Request - "Cannot update subjects in a closed semester"

---

#### ? **Test Case 20: Delete Subject Without Classes**
```http
DELETE /api/subjects/e7777777-7777-7777-7777-777777777777
Authorization: Bearer {admin-token}
```
**Expected:** 200 OK (NW201 has no classes)

---

#### ? **Test Case 21: Delete Subject With Classes (Should Fail)**
```http
DELETE /api/subjects/e1111111-1111-1111-1111-111111111111
Authorization: Bearer {admin-token}
```
**Expected:** 400 Bad Request - "Cannot delete subject that has existing classes"

---

#### ? **Test Case 22: Delete Subject from Closed Semester (Should Fail)**
```http
DELETE /api/subjects/e2222222-2222-2222-2222-222222222222
Authorization: Bearer {admin-token}
```
**Expected:** 400 Bad Request - "Cannot delete subjects from a closed semester"

---

## ?? Authentication

### Step 1: Login to get JWT token
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@fap.edu.vn",
  "password": "123456"
}
```

### Step 2: Use token in requests
```
Authorization: Bearer {your-jwt-token}
```

---

## ?? Expected Data Summary

### Pagination Examples

**Page 1 (Size 5):**
```http
GET /api/subjects?PageNumber=1&PageSize=5
```
Response:
- totalCount: 10
- pageNumber: 1
- pageSize: 5
- totalPages: 2
- data: [BC101, SE201, DB301, BC202, BC303]

**Page 2 (Size 5):**
```http
GET /api/subjects?PageNumber=2&PageSize=5
```
Response:
- data: [AI401, NW201, DS501, WEB301, ML501]

---

## ?? Quick Test Commands (PowerShell)

### Get all semesters
```powershell
$token = "your-jwt-token"
Invoke-RestMethod -Uri "https://localhost:7001/api/semesters" -Headers @{Authorization="Bearer $token"}
```

### Get semester details
```powershell
Invoke-RestMethod -Uri "https://localhost:7001/api/semesters/d1111111-1111-1111-1111-111111111111" -Headers @{Authorization="Bearer $token"}
```

### Get all subjects
```powershell
Invoke-RestMethod -Uri "https://localhost:7001/api/subjects" -Headers @{Authorization="Bearer $token"}
```

### Filter subjects by semester
```powershell
Invoke-RestMethod -Uri "https://localhost:7001/api/subjects?SemesterId=d3333333-3333-3333-3333-333333333333" -Headers @{Authorization="Bearer $token"}
```

---

## ?? Reset Database

If you need to reset and re-seed the database:

```bash
# Drop database
dotnet ef database drop --project Fap.Infrastructure --startup-project Fap.Api --force

# Apply migrations
dotnet ef database update --project Fap.Infrastructure --startup-project Fap.Api
```

The seeder will run automatically on first startup.

---

## ? Checklist

- [ ] Login as Admin
- [ ] Test GET /api/semesters
- [ ] Test GET /api/semesters with filters
- [ ] Test GET /api/semesters/{id}
- [ ] Test POST /api/semesters (success)
- [ ] Test POST /api/semesters (duplicate/overlap - should fail)
- [ ] Test PUT /api/semesters (open semester)
- [ ] Test PUT /api/semesters (closed semester - should fail)
- [ ] Test PATCH /api/semesters/{id}/close
- [ ] Test GET /api/subjects
- [ ] Test GET /api/subjects with filters
- [ ] Test GET /api/subjects/{id}
- [ ] Test POST /api/subjects (success)
- [ ] Test POST /api/subjects (duplicate code - should fail)
- [ ] Test POST /api/subjects (closed semester - should fail)
- [ ] Test PUT /api/subjects (open semester)
- [ ] Test PUT /api/subjects (closed semester - should fail)
- [ ] Test DELETE /api/subjects (no classes)
- [ ] Test DELETE /api/subjects (with classes - should fail)
- [ ] Test DELETE /api/subjects (closed semester - should fail)

---

## ?? Notes

- **Spring 2024** is marked as **closed** - use this to test closed semester restrictions
- **NW201** has no classes - use this to test successful subject deletion
- **BC101** has classes and is in closed semester - perfect for testing both deletion constraints
- Multiple subjects in same semester - test filtering and relationships
- Different credit values - test sorting by credits

**Happy Testing! ??**
