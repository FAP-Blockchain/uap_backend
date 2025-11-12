# Smart Contract Deployment Info

## ?? Deployed Contracts (Hardhat Local Network)

**Network**: Hardhat Local Node  
**URL**: http://127.0.0.1:8545  
**Chain ID**: 31337  
**Deployer Account**: 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266 (Hardhat #0)

---

## ?? Contract Addresses

| Contract Name | Address |
|--------------|---------|
| **UniversityManagement** | `0x5FbDB2315678afecb367f032d93F642f64180aa3` |
| **CredentialManagement** | `0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512` |
| **AttendanceManagement** | `0x9fE46736679d2D9a65F0992F2272dE9f3c7fa6e0` |
| **GradeManagement** | `0xCf7Ed3AccA5a467e9e704C703E8D87F634fB0Fc9` |
| **ClassManagement** | `0xDc64a140Aa3E981100a9becA4E685f962f0cF6C9` |

---

## ?? Test Account (Development Only)

**Private Key**: `0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80`  
**Address**: `0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266`  
**Balance**: 10,000 ETH (test ETH)

?? **WARNING**: This is Hardhat's default test account. **NEVER** use this in production!

---

## ?? Quick Start

### 1. Start Hardhat Node
```bash
npx hardhat node
```

### 2. Start API
```bash
cd Fap.Api
dotnet run
```

### 3. Test Blockchain Connection
```bash
curl https://localhost:7001/api/blockchain/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Expected response:
```json
{
  "success": true,
  "connected": true,
  "currentBlock": 0,
  "accountAddress": "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266",
  "timestamp": "2025-01-20T..."
}
```

---

## ?? Contract Functions

### CredentialManagement Contract
- `storeCredential(credentialId, studentCode, certificateHash)` - L?u credential
- `getCredential(credentialId)` - L?y thông tin credential
- `revokeCredential(credentialId)` - Thu h?i credential

### AttendanceManagement Contract
- `recordAttendance(studentId, slotId, status)` - Ghi nh?n ?i?m danh
- `getAttendance(studentId, slotId)` - L?y thông tin ?i?m danh

### GradeManagement Contract
- `recordGrade(studentId, subjectId, score)` - Ghi ?i?m
- `getGrade(studentId, subjectId)` - L?y ?i?m

### ClassManagement Contract
- `createClass(classCode, subjectId, teacherId)` - T?o l?p h?c
- `enrollStudent(classId, studentId)` - ??ng ký sinh viên vào l?p

### UniversityManagement Contract
- `addStudent(studentCode, fullName)` - Thêm sinh viên
- `addTeacher(teacherCode, fullName)` - Thêm gi?ng viên
- `addSubject(subjectCode, subjectName, credits)` - Thêm môn h?c

---

## ?? Testing with Swagger

1. Open Swagger UI: https://localhost:7001/swagger
2. Login to get JWT token:
   - Email: `admin@fap.edu.vn`
   - Password: `123456`
3. Click "Authorize" and paste token
4. Test `/api/blockchain/health` endpoint

---

## ?? Smart Contract Source

Các smart contract source code n?m trong th? m?c blockchain project (riêng bi?t).

**Contract Repository**: [Link to blockchain repo]

---

## ?? Redeploy Contracts

N?u c?n redeploy contracts:

```bash
# In blockchain project folder
npx hardhat run scripts/deploy.js --network localhost
```

Sau ?ó c?p nh?t addresses m?i vào `appsettings.json`:

```json
"BlockchainSettings": {
  "Contracts": {
    "UniversityManagement": "NEW_ADDRESS_HERE",
    "CredentialManagement": "NEW_ADDRESS_HERE",
    // ...
  }
}
```

---

**Last Updated**: January 2025  
**Deployment Date**: [Date when contracts were deployed]  
**Network**: Hardhat Local (Development)
