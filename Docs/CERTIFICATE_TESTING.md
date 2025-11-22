# 🧪 Certificate Testing Guide

## Test Data Overview

The `CredentialSeeder` creates 9 comprehensive test scenarios for the certificate sharing system.

---

## 📋 Test Cases

### ✅ **Case 1: Complete Issued Certificate**
- **Status**: Issued
- **Blockchain**: ✅ On-chain with transaction hash
- **QR Code**: ✅ Pre-generated
- **Share URL**: ✅ Available
- **Views**: 25
- **Purpose**: Test fully complete certificate with all features
- **Test**: 
  ```bash
  GET /api/credentials/public/{case1-id}
  # Should return complete data with QR code
  ```

---

### 🔄 **Case 2: Lazy QR Generation Test**
- **Status**: Issued
- **Blockchain**: ✅ On-chain
- **QR Code**: ❌ NULL (to be generated on first view)
- **Share URL**: ✅ Available
- **Views**: 0
- **Purpose**: Test lazy QR code generation
- **Test Flow**:
  1. First call to public endpoint should generate QR code
  2. `EnsureShareArtifactsAsync()` should create QR
  3. Database should be updated with QR code data
  4. Second call should return cached QR code
  ```bash
  GET /api/credentials/public/{case2-id}
  # First call: QR generated and saved
  # Second call: QR returned from DB
  ```

---

### 🚫 **Case 3: Revoked Certificate**
- **Status**: Revoked
- **Blockchain**: ❌ Not on-chain (or marked as revoked)
- **QR Code**: ❌ NULL
- **Share URL**: ❌ NULL
- **Views**: 5 (before revocation)
- **Revocation Reason**: "Certificate issued in error - student did not meet attendance requirements"
- **Purpose**: Test revoked certificate behavior
- **Expected**: 
  - Public endpoint should return 404 or revocation notice
  - Should NOT appear in student's shareable list
  ```bash
  GET /api/credentials/public/{case3-id}
  # Should return 404 or revoked status
  ```

---

### 📚 **Case 4: Semester Completion Certificate**
- **Status**: Issued
- **Type**: SemesterCompletion (not SubjectCompletion)
- **Blockchain**: ✅ On-chain
- **QR Code**: ✅ Pre-generated
- **Views**: 12
- **Classification**: "Good"
- **Purpose**: Test semester-level certificates
- **Test**: Different template and data structure

---

### 🌟 **Case 5: Popular Certificate (High Views)**
- **Status**: Issued
- **Blockchain**: ✅ On-chain
- **QR Code**: ✅ Pre-generated
- **Views**: **150** (very popular!)
- **Grade**: 9.5/10 (A+)
- **Purpose**: Test view count tracking
- **Test**: 
  ```bash
  GET /api/credentials/public/{case5-id}
  # ViewCount should increment to 151
  # LastViewedAt should update
  ```

---

### 🎓 **Case 6: Graduation Certificate**
- **Status**: Issued
- **Type**: RoadmapCompletion (Graduation)
- **Blockchain**: ✅ On-chain
- **QR Code**: ✅ Pre-generated
- **Views**: 45
- **Classification**: "Second Class Honours (Upper)"
- **Purpose**: Test graduation-level certificates
- **Test**: Different template for graduation

---

### ⏳ **Case 7: Pending Certificate**
- **Status**: **Pending** (not yet issued)
- **Blockchain**: ❌ Not on-chain
- **QR Code**: ❌ NULL
- **Share URL**: ❌ NULL
- **Views**: 0
- **IssuedDate**: NULL
- **Purpose**: Test pending certificate behavior
- **Expected**:
  - Should NOT be accessible via public endpoint
  - Should NOT appear in student share list
  - Only visible in admin/request management
  ```bash
  GET /api/credentials/public/{case7-id}
  # Should return 404 (not issued yet)
  ```

---

### 🆕 **Case 8: Recently Issued Certificate**
- **Status**: Issued
- **Blockchain**: ✅ On-chain
- **QR Code**: ✅ Pre-generated
- **Views**: 0 (brand new)
- **IssuedDate**: 1-3 days ago
- **Purpose**: Test newly issued certificates
- **Test**: Fresh certificate with no views yet

---

### ⏸️ **Case 9: Blockchain Pending**
- **Status**: Issued
- **Blockchain**: ⏳ **Pending** (IsOnBlockchain = false)
- **QR Code**: ✅ Pre-generated
- **Share URL**: ✅ Available
- **Views**: 3
- **Purpose**: Test blockchain verification pending state
- **Expected**:
  - Certificate is issued and viewable
  - Blockchain verification status shows "Pending"
  - `VerifyCertificateOnChainAsync()` should return false/pending
  ```bash
  GET /api/credentials/public/{case9-id}
  # Should return data with IsVerifiedOnChain: false
  ```

---

## 🧪 Testing Workflows

### **Workflow 1: Public Certificate View**
```http
GET /api/credentials/public/{credential-id}
Authorization: None (AllowAnonymous)

Response 200:
{
  "credentialId": "SUB-2025-000001",
  "studentName": "John Doe",
  "studentCode": "SE12345",
  "subjectName": "Advanced Programming",
  "semesterName": "Spring 2025",
  "finalGrade": 8.5,
  "letterGrade": "B+",
  "issuedDate": "2025-11-15",
  "templateName": "Subject Completion Certificate",
  "isVerifiedOnChain": true,
  "isRevoked": false,
  "verificationStatus": "Verified",
  "publicUrl": "http://localhost:3000/certificates/verify/SUB-2025-000001",
  "qrCodeData": "data:image/png;base64,iVBORw0K...",
  "viewCount": 26
}
```

### **Workflow 2: Student Share List**
```http
GET /api/students/me/certificates/share
Authorization: Bearer {student-jwt-token}

Response 200:
[
  {
    "credentialId": "SUB-2025-000001",
    "subjectName": "Advanced Programming",
    "isVerifiedOnChain": true,
    "publicUrl": "http://localhost:3000/certificates/verify/SUB-2025-000001",
    "qrCodeData": "data:image/png;base64,...",
    "viewCount": 25
  },
  {
    "credentialId": "GRAD-2025-000006",
    "templateName": "Graduation Certificate",
    "isVerifiedOnChain": true,
    "publicUrl": "http://localhost:3000/certificates/verify/GRAD-2025-000006",
    "qrCodeData": "data:image/png;base64,...",
    "viewCount": 45
  }
]
```

### **Workflow 3: QR Code Scanning**
1. Student gets QR code from `/api/students/me/certificates/share`
2. QR code contains URL: `http://localhost:3000/certificates/verify/{id}`
3. Anyone scans QR code → redirects to frontend
4. Frontend calls `/api/credentials/public/{id}`
5. ViewCount increments automatically
6. Public can verify certificate authenticity

---

## 🔧 How to Re-seed Data

### Option 1: Drop & Recreate Database (Recommended for Dev)
```powershell
# Run the seeding script
.\Scripts\reseed-credentials.ps1

# Choose option 1: Drop and recreate database
# This will:
# 1. Drop existing database
# 2. Run all migrations
# 3. Restart app to trigger seeding
```

### Option 2: Manual SQL Cleanup
```sql
BEGIN TRANSACTION;

DELETE FROM ActionLogs WHERE CredentialId IS NOT NULL;
DELETE FROM Credentials;
DELETE FROM CredentialRequests;

COMMIT;
```

Then restart the application:
```powershell
dotnet run --project Fap.Api
```

---

## 📊 Expected Seeder Output

```
🌱 Seeding Credentials...
   ✅ Created 9 credentials:
      • Issued: 7
      • Pending: 1
      • Revoked: 1
      • With QR Code: 6
      • Without QR (Lazy Gen): 1
      • On Blockchain: 6
      • Blockchain Pending: 1
      • With Views: 5
```

---

## 🎯 Test Checklist

- [ ] **Public View**: Call public endpoint for Case 1-9
- [ ] **Lazy QR Generation**: Verify Case 2 generates QR on first call
- [ ] **View Count**: Verify viewCount increments on each call
- [ ] **Revoked Certificate**: Verify Case 3 returns 404 or error
- [ ] **Pending Certificate**: Verify Case 7 is not publicly accessible
- [ ] **Student Share List**: Login as student and check share list
- [ ] **Blockchain Verification**: Check Case 9 shows pending status
- [ ] **QR Code Format**: Verify QR code is valid base64 PNG
- [ ] **Frontend URL**: Verify ShareableUrl uses correct frontend URL
- [ ] **Different Templates**: Test Case 4 (Semester) and Case 6 (Graduation)

---

## 🐛 Troubleshooting

### QR Code not generating?
- Check `QRCoder` package is installed
- Verify `FrontendSettings` is configured in appsettings.json
- Check service injection in `Program.cs`

### Blockchain verification fails?
- Check `BlockchainService.VerifyCertificateOnChainAsync()` implementation
- Verify test data has valid transaction hashes (Case 1-8)
- Case 9 should intentionally have no blockchain data

### ShareableUrl is null?
- Only Issued, non-revoked certificates have ShareableUrl
- Pending (Case 7) and Revoked (Case 3) should have NULL

---

## 📚 Related Files

- **Seeder**: `Fap.Infrastructure/Data/Seed/CredentialSeeder.cs`
- **Service**: `Fap.Api/Services/CredentialService.cs`
- **Controller**: `Fap.Api/Controllers/CredentialsController.cs`
- **Settings**: `Fap.Api/appsettings.json` → FrontendSettings
- **DTO**: `Fap.Domain/DTOs/Credential/CredentialDtos.cs`

---

**Happy Testing! 🧪✨**
