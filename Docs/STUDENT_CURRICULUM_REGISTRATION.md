# 📚 Student Registration with Curriculum

## Tổng quan

Khi đăng ký tài khoản Student, **hệ thống yêu cầu gắn Curriculum** để:
- Tạo lộ trình học tập (roadmap) tự động
- Kiểm tra điều kiện tiên quyết khi đăng ký môn học
- Đề xuất môn học phù hợp theo semester
- Đánh giá điều kiện tốt nghiệp

---

## 🔧 API Endpoint

### Single Registration

**POST** `/api/auth/register`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "fullName": "Nguyen Van A",
  "email": "student@example.com",
  "password": "SecurePass123",
  "roleName": "Student",
  "studentCode": "SE170001",
  "enrollmentDate": "2024-09-01",
  "curriculumId": 1,  // ✅ REQUIRED for Student - links to curriculum
  "walletAddress": "0x1234567890abcdef1234567890abcdef12345678"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": "guid",
    "email": "student@example.com",
    "roleName": "Student"
  },
  "blockchainInfo": {
    "walletAddress": "0x...",
    "transactionHash": "0x...",
    "blockNumber": 12345,
    "registeredAt": "2024-11-22T10:00:00Z"
  }
}
```

---

### Bulk Registration

**POST** `/api/auth/register/bulk`

**Request Body:**
```json
{
  "users": [
    {
      "fullName": "Nguyen Van A",
      "email": "student1@example.com",
      "password": "Pass123",
      "roleName": "Student",
      "studentCode": "SE170001",
      "enrollmentDate": "2024-09-01",
      "curriculumId": 1  // Software Engineering 2024
    },
    {
      "fullName": "Tran Thi B",
      "email": "student2@example.com",
      "password": "Pass456",
      "roleName": "Student",
      "studentCode": "DS170002",
      "enrollmentDate": "2024-09-01",
      "curriculumId": 2  // Data Science 2024
    }
  ]
}
```

---

## 📋 Available Curriculums

### 1. Software Engineering 2024 (ID: 1)
- **Code:** `SE-2024`
- **Total Credits:** 120
- **Subjects:** CS101, MATH101, SE101, SE102, CS201, DB201, WEB301...
- **Description:** Four-year curriculum focused on software development, quality, and architecture

### 2. Data Science 2024 (ID: 2)
- **Code:** `DS-2024`
- **Total Credits:** 118
- **Subjects:** CS101, MATH101, DB201, CS201, MATH201, SE101, SE102, WEB301...
- **Description:** Applied data science curriculum with analytics, programming, and database foundations

---

## ✅ Validation Rules

### Khi đăng ký Student:

1. **Required Fields:**
   - `studentCode` - Mã sinh viên (unique)
   - `roleName` - Phải là "Student"

2. **Optional but Recommended:**
   - `curriculumId` - Liên kết với chương trình đào tạo
   - `enrollmentDate` - Ngày nhập học (mặc định: hôm nay)

3. **CurriculumId Validation:**
   - Nếu có `curriculumId`, hệ thống sẽ kiểm tra curriculum có tồn tại không
   - Nếu không có `curriculumId`, student vẫn được tạo nhưng:
     - ⚠️ Không thể dùng API `/curriculum-roadmap`
     - ⚠️ Không thể đánh giá tốt nghiệp
     - ⚠️ Không có đề xuất môn học tự động

---

## 🔄 Workflow

```
Admin Register Student
    ↓
System validates CurriculumId (if provided)
    ↓
Create User + Student record with CurriculumId
    ↓
Register on Blockchain
    ↓
Student can view curriculum roadmap
    ↓
System suggests subjects based on:
    - Semester number
    - Prerequisites
    - Current progress
```

---

## 📊 Database Schema

```sql
CREATE TABLE Students (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    StudentCode NVARCHAR(30) NOT NULL UNIQUE,
    EnrollmentDate DATETIME2 NOT NULL,
    CurriculumId INT NULL,  -- ✅ Links to Curriculum
    GPA DECIMAL(3,2) DEFAULT 0,
    IsGraduated BIT DEFAULT 0,
    GraduationDate DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (CurriculumId) REFERENCES Curriculums(Id)
);
```

---

## 🎯 Related APIs

### 1. Get My Curriculum Roadmap
```http
GET /api/students/me/curriculum-roadmap
```

### 2. Check Graduation Status
```http
GET /api/students/me/graduation-status
```

### 3. Get Subject Recommendations
```http
GET /api/students/me/roadmap/recommendations
```

---

## ⚠️ Important Notes

1. **CurriculumId is Optional** - Student có thể được tạo mà không có curriculum, nhưng sẽ bị giới hạn chức năng
2. **Admin can update later** - Admin có thể gắn curriculum sau thông qua API update student
3. **Legacy Students** - Students không có curriculum vẫn hoạt động bình thường với legacy eligibility check

---

## 🧪 Testing

### Test Case 1: Register with Curriculum
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test Student",
    "email": "test@example.com",
    "password": "Test123",
    "roleName": "Student",
    "studentCode": "TEST001",
    "curriculumId": 1
  }'
```

### Test Case 2: Register without Curriculum
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test Student 2",
    "email": "test2@example.com",
    "password": "Test123",
    "roleName": "Student",
    "studentCode": "TEST002"
  }'
```

### Test Case 3: Invalid CurriculumId
```bash
# Expected Error: "Curriculum with ID '999' not found"
curl -X POST http://localhost:5000/api/auth/register \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test Student 3",
    "email": "test3@example.com",
    "password": "Test123",
    "roleName": "Student",
    "studentCode": "TEST003",
    "curriculumId": 999
  }'
```

---

## 📖 See Also

- [Student Roadmap API](../Fap.Api/Controllers/StudentRoadmapController.cs)
- [Curriculum Entities](../Fap.Domain/Entities/Curriculum.cs)
- [Curriculum Seeder](../Fap.Infrastructure/Data/Seed/CurriculumSeeder.cs)
