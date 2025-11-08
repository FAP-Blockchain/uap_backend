# ?? Quick Start Guide - Testing Subjects & Semesters API

## ?? Prerequisites

- ? .NET 8 SDK installed
- ? SQL Server running
- ? Connection string configured in `appsettings.json`

---

## ?? Setup Steps

### 1?? **Apply Database Migration**

Open terminal in project root and run:

```bash
dotnet ef database update --project Fap.Infrastructure --startup-project Fap.Api
```

This will:
- Create database structure
- Add `IsClosed` column to Semesters table

---

### 2?? **Run Application**

```bash
cd Fap.Api
dotnet run
```

Or press **F5** in Visual Studio.

The application will:
- ? Apply migrations automatically
- ? Seed test data (if database is empty)
- ? Start on `https://localhost:7001` (or configured port)

Expected console output:
```
?? Applying migrations...
? Database migration & seeding done!
```

---

### 3?? **Access Swagger UI**

Open browser and navigate to:
```
https://localhost:7001/swagger
```

---

## ?? Step-by-Step Testing

### **Step 1: Login as Admin**

1. In Swagger, find **POST /api/auth/login**
2. Click "Try it out"
3. Enter request body:
```json
{
  "email": "admin@fap.edu.vn",
  "password": "123456"
}
```
4. Click "Execute"
5. **Copy the `accessToken`** from response

---

### **Step 2: Authorize in Swagger**

1. Click the **"Authorize"** button (?? icon at top right)
2. Enter: `Bearer {paste-your-token-here}`
3. Click "Authorize"
4. Click "Close"

? Now all requests will include your token!

---

### **Step 3: Test Semesters API**

#### ?? **Get All Semesters**
- Endpoint: **GET /api/semesters**
- Click "Try it out" ? "Execute"
- **Expected:** List of 5 semesters

#### ?? **Get Semester Details**
- Endpoint: **GET /api/semesters/{id}**
- Use ID: `d1111111-1111-1111-1111-111111111111` (Spring 2024 - Closed)
- **Expected:** 
  - 2 subjects
  - 2 classes
  - 5 students
  - `isClosed: true`

#### ?? **Filter Closed Semesters**
- Endpoint: **GET /api/semesters**
- Set parameter: `IsClosed = true`
- **Expected:** Only Spring 2024

#### ?? **Search Semesters**
- Endpoint: **GET /api/semesters**
- Set parameter: `SearchTerm = 2024`
- **Expected:** 3 semesters (Spring, Summer, Fall 2024)

#### ?? **Create New Semester**
- Endpoint: **POST /api/semesters**
- Request body:
```json
{
  "name": "Winter 2026",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-03-31T00:00:00Z"
}
```
- **Expected:** 201 Created

#### ?? **Try Creating Overlapping Semester (Should Fail)**
- Endpoint: **POST /api/semesters**
- Request body:
```json
{
  "name": "Test Overlap",
  "startDate": "2024-01-20T00:00:00Z",
  "endDate": "2024-05-10T00:00:00Z"
}
```
- **Expected:** 400 Bad Request - "The date range overlaps..."

#### ?? **Try Updating Closed Semester (Should Fail)**
- Endpoint: **PUT /api/semesters/{id}**
- ID: `d1111111-1111-1111-1111-111111111111`
- Request body:
```json
{
  "name": "Spring 2024 Updated",
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2024-05-15T00:00:00Z"
}
```
- **Expected:** 400 Bad Request - "Cannot update a closed semester"

---

### **Step 4: Test Subjects API**

#### ?? **Get All Subjects**
- Endpoint: **GET /api/subjects**
- **Expected:** List of 10 subjects

#### ?? **Filter by Semester**
- Endpoint: **GET /api/subjects**
- Set parameter: `SemesterId = d3333333-3333-3333-3333-333333333333` (Fall 2024)
- **Expected:** 3 subjects (BC303, AI401, NW201)

#### ?? **Search Subjects**
- Endpoint: **GET /api/subjects**
- Set parameter: `SearchTerm = blockchain`
- **Expected:** BC101, BC202, BC303

#### ?? **Sort by Credits**
- Endpoint: **GET /api/subjects**
- Set parameters:
  - `SortBy = Credits`
  - `IsDescending = true`
- **Expected:** 4-credit subjects first

#### ?? **Get Subject Details**
- Endpoint: **GET /api/subjects/{id}**
- Use ID: `e1111111-1111-1111-1111-111111111111` (BC101)
- **Expected:**
  - Subject info
  - 1 class (BC101-A)
  - Teacher: Nguy?n V?n Giáo Viên
  - 3 students enrolled

#### ?? **Create New Subject**
- Endpoint: **POST /api/subjects**
- Request body:
```json
{
  "subjectCode": "TEST101",
  "subjectName": "Test Subject",
  "credits": 3,
  "semesterId": "d4444444-4444-4444-4444-444444444444"
}
```
- **Expected:** 201 Created

#### ?? **Try Creating Duplicate Code (Should Fail)**
- Endpoint: **POST /api/subjects**
- Request body:
```json
{
  "subjectCode": "BC101",
  "subjectName": "Duplicate",
  "credits": 3,
  "semesterId": "d4444444-4444-4444-4444-444444444444"
}
```
- **Expected:** 400 Bad Request - "Subject with code 'BC101' already exists"

#### ?? **Try Adding Subject to Closed Semester (Should Fail)**
- Endpoint: **POST /api/subjects**
- Request body:
```json
{
  "subjectCode": "NEW101",
  "subjectName": "New Subject",
  "credits": 3,
  "semesterId": "d1111111-1111-1111-1111-111111111111"
}
```
- **Expected:** 400 Bad Request - "Cannot add subjects to a closed semester"

#### ?? **Delete Subject Without Classes**
- Endpoint: **DELETE /api/subjects/{id}**
- Use ID: `e7777777-7777-7777-7777-777777777777` (NW201 - no classes)
- **Expected:** 200 OK

#### ?? **Try Deleting Subject With Classes (Should Fail)**
- Endpoint: **DELETE /api/subjects/{id}**
- Use ID: `e1111111-1111-1111-1111-111111111111` (BC101 - has classes)
- **Expected:** 400 Bad Request - "Cannot delete subject that has existing classes"

---

## ?? Test Data Summary

### Available Test Accounts

| Email | Password | Role |
|-------|----------|------|
| admin@fap.edu.vn | 123456 | Admin |
| teacher1@fap.edu.vn | 123456 | Teacher |
| student1@fap.edu.vn | 123456 | Student |

### Semesters (5)

| Name | Status | Subjects | Classes |
|------|--------|----------|---------|
| Spring 2024 | ? **Closed** | 2 | 2 |
| Summer 2024 | Active | 2 | 3 |
| Fall 2024 | Active | 3 | 2 |
| Spring 2025 | Future | 2 | 0 |
| Fall 2025 | Future | 1 | 0 |

### Subjects (10)

Distributed across 5 semesters, ranging from 3-4 credits each.

### Special Test Cases

- **Spring 2024**: Closed semester - use for testing restrictions
- **NW201**: No classes - use for testing successful deletion
- **BC101**: Has classes + in closed semester - test both constraints

---

## ?? Troubleshooting

### Issue: "Database already exists"
**Solution:** Drop and recreate:
```bash
dotnet ef database drop --project Fap.Infrastructure --startup-project Fap.Api --force
dotnet ef database update --project Fap.Infrastructure --startup-project Fap.Api
```

### Issue: "No data returned"
**Solution:** Check if seeder ran. Look for console message: `? Database migration & seeding done!`

### Issue: "401 Unauthorized"
**Solution:**
1. Login again to get fresh token
2. Click "Authorize" button in Swagger
3. Enter: `Bearer {your-token}`

### Issue: "403 Forbidden"
**Solution:** Some endpoints require Admin role. Make sure you're logged in as `admin@fap.edu.vn`

---

## ?? Notes

- All passwords are: `123456`
- Tokens expire after configured time
- Closed semester = `IsClosed = true`
- All dates are in UTC
- IDs are fixed GUIDs for easy testing

---

## ? Quick Validation Checklist

After setup, verify:

- [ ] Application starts without errors
- [ ] Swagger UI loads at https://localhost:7001/swagger
- [ ] Can login as admin
- [ ] GET /api/semesters returns 5 items
- [ ] GET /api/subjects returns 10 items
- [ ] Can create new semester
- [ ] Can create new subject
- [ ] Closed semester restrictions work
- [ ] Duplicate validation works
- [ ] Delete with constraints works

---

## ?? Next Steps

1. ? Complete all test cases in `TEST_DATA_GUIDE.md`
2. ? Verify pagination works correctly
3. ? Test all filters and sorting options
4. ? Verify business rules enforcement
5. ? Check authorization on all endpoints

---

**Happy Testing! ??**

If you encounter any issues, check:
- `API_DOCUMENTATION.md` for endpoint details
- `TEST_DATA_GUIDE.md` for comprehensive test scenarios
- `IMPLEMENTATION_SUMMARY.md` for implementation overview
