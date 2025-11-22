# 📚 Curriculum Management API

## Overview

Complete CRUD API for managing curriculums and their subject mappings. Admin-only endpoints for curriculum lifecycle management.

---

## 🔐 Authorization

**All endpoints require Admin role:**
```
Authorization: Bearer {admin_token}
```

---

## 📋 API Endpoints

### 1. Get All Curriculums

**GET** `/api/curriculum`

**Description:** Retrieve all curriculums with basic information

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 2 curriculums",
  "data": [
    {
      "id": 1,
      "code": "SE-2024",
      "name": "Software Engineering 2024",
      "description": "Four-year curriculum focused on software development",
      "totalCredits": 120,
      "subjectCount": 10,
      "studentCount": 25
    },
    {
      "id": 2,
      "code": "DS-2024",
      "name": "Data Science 2024",
      "description": "Applied data science curriculum",
      "totalCredits": 118,
      "subjectCount": 12,
      "studentCount": 18
    }
  ]
}
```

---

### 2. Get Curriculum by ID

**GET** `/api/curriculum/{id}`

**Description:** Get detailed curriculum information including all subjects and prerequisites

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "code": "SE-2024",
    "name": "Software Engineering 2024",
    "description": "Four-year curriculum focused on software development",
    "totalCredits": 120,
    "studentCount": 25,
    "subjects": [
      {
        "id": 1,
        "subjectId": "guid",
        "subjectCode": "CS101",
        "subjectName": "Introduction to Programming",
        "credits": 3,
        "semesterNumber": 1,
        "prerequisiteSubjectId": null,
        "prerequisiteSubjectCode": null,
        "prerequisiteSubjectName": null
      },
      {
        "id": 2,
        "subjectId": "guid",
        "subjectCode": "SE101",
        "subjectName": "Software Engineering Principles",
        "credits": 3,
        "semesterNumber": 2,
        "prerequisiteSubjectId": "guid",
        "prerequisiteSubjectCode": "CS101",
        "prerequisiteSubjectName": "Introduction to Programming"
      }
    ]
  }
}
```

---

### 3. Create Curriculum

**POST** `/api/curriculum`

**Request Body:**
```json
{
  "code": "AI-2024",
  "name": "Artificial Intelligence 2024",
  "description": "Comprehensive AI and Machine Learning program",
  "totalCredits": 125
}
```

**Validation Rules:**
- `code`: Required, max 64 characters, must be unique
- `name`: Required, max 128 characters
- `description`: Optional, max 512 characters
- `totalCredits`: Required, must be between 1 and 300

**Response:**
```json
{
  "success": true,
  "message": "Curriculum created successfully",
  "data": {
    "id": 3,
    "code": "AI-2024",
    "name": "Artificial Intelligence 2024",
    "description": "Comprehensive AI and Machine Learning program",
    "totalCredits": 125,
    "subjectCount": 0,
    "studentCount": 0
  }
}
```

**Error Responses:**
```json
// Code already exists
{
  "success": false,
  "message": "Curriculum with code 'AI-2024' already exists"
}
```

---

### 4. Update Curriculum

**PUT** `/api/curriculum/{id}`

**Request Body:**
```json
{
  "code": "SE-2024-V2",
  "name": "Software Engineering 2024 - Updated",
  "description": "Updated curriculum with new requirements",
  "totalCredits": 125
}
```

**Response:**
```json
{
  "success": true,
  "message": "Curriculum updated successfully",
  "data": {
    "id": 1,
    "code": "SE-2024-V2",
    "name": "Software Engineering 2024 - Updated",
    "description": "Updated curriculum with new requirements",
    "totalCredits": 125,
    "subjectCount": 10,
    "studentCount": 25
  }
}
```

**Error Responses:**
```json
// Curriculum not found
{
  "success": false,
  "message": "Curriculum with ID 999 not found"
}

// Code already exists
{
  "success": false,
  "message": "Curriculum with code 'SE-2024-V2' already exists"
}
```

---

### 5. Delete Curriculum

**DELETE** `/api/curriculum/{id}`

**Response:**
```json
{
  "success": true,
  "message": "Curriculum deleted successfully"
}
```

**Error Responses:**
```json
// Curriculum in use
{
  "success": false,
  "message": "Cannot delete curriculum. It is currently assigned to 25 student(s)"
}

// Curriculum not found
{
  "success": false,
  "message": "Curriculum with ID 999 not found"
}
```

**Notes:**
- Cannot delete curriculum if it's assigned to any students
- All curriculum subjects will be deleted (cascade delete)

---

### 6. Add Subject to Curriculum

**POST** `/api/curriculum/{id}/subjects`

**Description:** Add a subject to curriculum with semester and prerequisite information

**Request Body:**
```json
{
  "subjectId": "guid",
  "semesterNumber": 2,
  "prerequisiteSubjectId": "guid"  // Optional
}
```

**Validation Rules:**
- `subjectId`: Required, must exist in Subjects table
- `semesterNumber`: Required, must be between 1 and 20
- `prerequisiteSubjectId`: Optional, must exist in curriculum if provided

**Response:**
```json
{
  "success": true,
  "message": "Subject 'SE101' added to curriculum successfully",
  "data": {
    "id": 15,
    "subjectId": "guid",
    "subjectCode": "SE101",
    "subjectName": "Software Engineering Principles",
    "credits": 3,
    "semesterNumber": 2,
    "prerequisiteSubjectId": "guid"
  }
}
```

**Error Responses:**
```json
// Curriculum not found
{
  "success": false,
  "message": "Curriculum with ID 999 not found"
}

// Subject not found
{
  "success": false,
  "message": "Subject with ID {guid} not found"
}

// Subject already in curriculum
{
  "success": false,
  "message": "Subject 'SE101' is already in this curriculum"
}

// Prerequisite not in curriculum
{
  "success": false,
  "message": "Prerequisite subject 'CS101' must be added to curriculum first"
}
```

---

### 7. Remove Subject from Curriculum

**DELETE** `/api/curriculum/{id}/subjects/{subjectId}`

**Description:** Remove a subject from curriculum

**Response:**
```json
{
  "success": true,
  "message": "Subject removed from curriculum successfully"
}
```

**Error Responses:**
```json
// Curriculum not found
{
  "success": false,
  "message": "Curriculum with ID 999 not found"
}

// Subject not in curriculum
{
  "success": false,
  "message": "Subject not found in this curriculum"
}

// Subject is prerequisite for others
{
  "success": false,
  "message": "Cannot remove subject. It is a prerequisite for: SE102, CS201"
}
```

**Notes:**
- Cannot remove subject if it's a prerequisite for other subjects in the curriculum
- Remove dependent subjects first, then remove the prerequisite

---

## 🔄 Common Workflows

### Workflow 1: Create New Curriculum

```bash
# Step 1: Create curriculum
POST /api/curriculum
{
  "code": "AI-2024",
  "name": "Artificial Intelligence 2024",
  "totalCredits": 120
}

# Step 2: Add subjects for Semester 1
POST /api/curriculum/3/subjects
{
  "subjectId": "{CS101-guid}",
  "semesterNumber": 1
}

POST /api/curriculum/3/subjects
{
  "subjectId": "{MATH101-guid}",
  "semesterNumber": 1
}

# Step 3: Add subjects for Semester 2 with prerequisites
POST /api/curriculum/3/subjects
{
  "subjectId": "{CS201-guid}",
  "semesterNumber": 2,
  "prerequisiteSubjectId": "{CS101-guid}"
}
```

---

### Workflow 2: Update Curriculum Structure

```bash
# Step 1: Get current curriculum
GET /api/curriculum/1

# Step 2: Update basic info
PUT /api/curriculum/1
{
  "code": "SE-2024-V2",
  "name": "Software Engineering 2024 - Updated",
  "totalCredits": 125
}

# Step 3: Add new subject
POST /api/curriculum/1/subjects
{
  "subjectId": "{NEW-SUBJECT-guid}",
  "semesterNumber": 3
}

# Step 4: Remove old subject (if not prerequisite)
DELETE /api/curriculum/1/subjects/{OLD-SUBJECT-guid}
```

---

### Workflow 3: Safe Curriculum Deletion

```bash
# Step 1: Check curriculum details
GET /api/curriculum/1

# Step 2: If studentCount > 0, cannot delete
# Response: "Cannot delete curriculum. It is currently assigned to 25 student(s)"

# Option A: Reassign all students to different curriculum first
# Option B: Keep the curriculum

# Step 3: If studentCount = 0, safe to delete
DELETE /api/curriculum/1
```

---

## 📊 Database Schema

```sql
-- Curriculum table
CREATE TABLE Curriculums (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(64) NOT NULL UNIQUE,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(512) NULL,
    TotalCredits INT NOT NULL
);

-- CurriculumSubject junction table
CREATE TABLE CurriculumSubjects (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CurriculumId INT NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    SemesterNumber INT NOT NULL,
    PrerequisiteSubjectId UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (CurriculumId) REFERENCES Curriculums(Id) ON DELETE CASCADE,
    FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE RESTRICT,
    FOREIGN KEY (PrerequisiteSubjectId) REFERENCES Subjects(Id) ON DELETE RESTRICT,
    UNIQUE (CurriculumId, SubjectId)
);

-- Student reference
ALTER TABLE Students ADD CurriculumId INT NULL;
ALTER TABLE Students ADD CONSTRAINT FK_Students_Curriculums 
    FOREIGN KEY (CurriculumId) REFERENCES Curriculums(Id) ON DELETE RESTRICT;
```

---

## 🧪 Testing Examples

### Test 1: Create Complete Curriculum

```bash
# Create curriculum
curl -X POST http://localhost:5000/api/curriculum \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TEST-2024",
    "name": "Test Curriculum",
    "totalCredits": 100
  }'

# Add subjects
curl -X POST http://localhost:5000/api/curriculum/3/subjects \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "subjectId": "{subject-guid}",
    "semesterNumber": 1
  }'
```

---

### Test 2: Validate Prerequisite Chain

```bash
# Try to remove prerequisite subject (should fail)
curl -X DELETE http://localhost:5000/api/curriculum/1/subjects/{CS101-guid} \
  -H "Authorization: Bearer {admin_token}"

# Expected Error: "Cannot remove subject. It is a prerequisite for: SE101, CS201"
```

---

### Test 3: Prevent Duplicate Subjects

```bash
# Try to add same subject twice (should fail)
curl -X POST http://localhost:5000/api/curriculum/1/subjects \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "subjectId": "{already-added-subject-guid}",
    "semesterNumber": 2
  }'

# Expected Error: "Subject 'CS101' is already in this curriculum"
```

---

## ⚠️ Important Notes

### Business Rules

1. **Curriculum Code Uniqueness**: Each curriculum must have a unique code
2. **Cascade Delete**: Deleting curriculum removes all CurriculumSubjects
3. **Student Protection**: Cannot delete curriculum assigned to students
4. **Prerequisite Order**: Prerequisites must be added before dependent subjects
5. **Prerequisite Removal**: Cannot remove subject if it's a prerequisite

### Data Integrity

- Curriculum → CurriculumSubjects: CASCADE DELETE
- CurriculumSubjects → Subject: RESTRICT DELETE
- Student → Curriculum: RESTRICT DELETE
- CurriculumSubjects (CurriculumId, SubjectId): UNIQUE constraint

---

## 🎯 Related APIs

- [Student Registration with Curriculum](STUDENT_CURRICULUM_REGISTRATION.md)
- [Student Roadmap API](../Fap.Api/Controllers/StudentRoadmapController.cs)
- [Subject Management](../Fap.Api/Controllers/SubjectsController.cs)

---

## 📖 See Also

- [Curriculum Entity](../Fap.Domain/Entities/Curriculum.cs)
- [CurriculumSubject Entity](../Fap.Domain/Entities/CurriculumSubject.cs)
- [Curriculum Seeder](../Fap.Infrastructure/Data/Seed/CurriculumSeeder.cs)
