# Blockchain Integration Guide

## ?? T?ng quan

D? án ?ã ???c tích h?p blockchain s? d?ng **Nethereum** library ?? t??ng tác v?i Ethereum-compatible blockchain (Hardhat local, Sepolia testnet, Ethereum mainnet...).

## ??? Ki?n trúc Blockchain

### **1. BlockchainSettings** (`Fap.Domain/Settings/BlockchainSettings.cs`)
```csharp
- NetworkUrl: URL c?a blockchain node (e.g., http://127.0.0.1:8545 cho Hardhat)
- ChainId: Chain ID (31337 cho Hardhat local, 11155111 cho Sepolia)
- PrivateKey: Private key ?? sign transactions
- Contracts: ??a ch? các smart contracts
  - UniversityManagement
  - CredentialManagement
  - AttendanceManagement
  - GradeManagement
  - ClassManagement
- GasLimit: Gi?i h?n gas (default: 3,000,000)
- GasPrice: Gas price tính b?ng Wei (default: 20 Gwei)
- TransactionTimeout: Timeout cho transaction confirmation (seconds)
```

### **2. IBlockchainService** (`Fap.Api/Interfaces/IBlockchainService.cs`)
Interface ??nh ngh?a các method:
- `IsConnectedAsync()`: Ki?m tra k?t n?i blockchain
- `GetCurrentBlockNumberAsync()`: L?y block number hi?n t?i
- `GetCurrentAccountAddress()`: L?y ??a ch? account ?ang s? d?ng
- `StoreCredentialAsync()`: L?u credential lên blockchain
- `VerifyCredentialAsync()`: Verify transaction
- `GetCredentialFromBlockchainAsync()`: L?y thông tin credential t? blockchain

### **3. BlockchainService** (`Fap.Api/Services/BlockchainService.cs`)
Implementation c?a IBlockchainService s? d?ng Nethereum

### **4. BlockchainController** (`Fap.Api/Controllers/BlockchainController.cs`)
REST API endpoints:
- `GET /api/blockchain/health`: Check blockchain connection
- `POST /api/blockchain/store-credential`: L?u credential lên blockchain
- `GET /api/blockchain/verify/{transactionHash}`: Verify credential
- `GET /api/blockchain/credential/{transactionHash}`: L?y credential info

### **5. Credential Entity Updates** (`Fap.Domain/Entities/Credential.cs`)
?ã thêm 3 fields m?i:
- `BlockchainTransactionHash`: Transaction hash trên blockchain
- `BlockchainStoredAt`: Th?i gian l?u lên blockchain
- `IsOnBlockchain`: Flag ki?m tra ?ã l?u lên blockchain ch?a

## ?? H??ng d?n Setup

### **B??c 1: C?u hình appsettings.json**
```json
"BlockchainSettings": {
  "NetworkUrl": "http://127.0.0.1:8545",
  "ChainId": 31337,
  "PrivateKey": "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80",
  "Contracts": {
    "CredentialManagement": "0x5FbDB2315678afecb367f032d93F642f64180aa3",
    "UniversityManagement": "0x...",
    "AttendanceManagement": "0x...",
    "GradeManagement": "0x...",
    "ClassManagement": "0x..."
  },
  "GasLimit": 3000000,
  "GasPrice": 20000000000,
  "TransactionTimeout": 60
}
```

?? **L?U Ý**: 
- Private key ? trên là **test account #0 c?a Hardhat** - CH? dùng cho development
- KHÔNG bao gi? commit private key th?t lên Git
- Trong production, dùng Azure Key Vault ho?c environment variables

### **B??c 2: Ch?y Hardhat Local Node**
```bash
# Trong th? m?c blockchain project
npx hardhat node
```

Hardhat s?:
- Start local blockchain t?i `http://127.0.0.1:8545`
- ChainId: 31337
- T?o 20 test accounts v?i 10,000 ETH m?i account

### **B??c 3: Deploy Smart Contracts**
```bash
# Deploy contracts lên Hardhat local
npx hardhat run scripts/deploy.js --network localhost
```

Sau khi deploy xong, copy contract addresses vào `appsettings.json`

### **B??c 4: Create Migration cho Credential Table**
```bash
cd Fap.Infrastructure
dotnet ef migrations add AddBlockchainFieldsToCredential --startup-project ../Fap.Api
dotnet ef database update --startup-project ../Fap.Api
```

### **B??c 5: Ch?y API**
```bash
cd Fap.Api
dotnet run
```

## ?? Testing Blockchain Integration

### **1. Ki?m tra k?t n?i**
```bash
curl -X GET "https://localhost:7001/api/blockchain/health" -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Response:
```json
{
  "success": true,
  "connected": true,
  "currentBlock": 12345,
  "accountAddress": "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266",
  "timestamp": "2025-01-20T10:30:00Z"
}
```

### **2. L?u credential lên blockchain**
```bash
curl -X POST "https://localhost:7001/api/blockchain/store-credential" \
-H "Authorization: Bearer YOUR_JWT_TOKEN" \
-H "Content-Type: application/json" \
-d '{
  "credentialId": "d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4",
  "studentCode": "SV001",
  "certificateHash": "QmYwAPJzv5CZsnA625s3Xf2nemtYgPpHdWEz79ojWnPbdG"
}'
```

Response:
```json
{
  "success": true,
  "transactionHash": "0x1234567890abcdef...",
  "message": "Credential stored on blockchain successfully",
  "timestamp": "2025-01-20T10:31:00Z"
}
```

### **3. Verify credential**
```bash
curl -X GET "https://localhost:7001/api/blockchain/verify/0x1234567890abcdef..."
```

Response:
```json
{
  "success": true,
  "transactionHash": "0x1234567890abcdef...",
  "isValid": true,
  "message": "Credential is valid",
  "timestamp": "2025-01-20T10:32:00Z"
}
```

## ?? Smart Contract Example (Solidity)

```solidity
// contracts/CredentialManagement.sol
pragma solidity ^0.8.0;

contract CredentialManagement {
    struct Credential {
        string credentialId;
        string studentCode;
        string certificateHash;
        address issuer;
        uint256 issuedAt;
        bool isRevoked;
    }

    mapping(string => Credential) public credentials;
    
    event CredentialStored(
        string credentialId,
        string studentCode,
        string certificateHash,
        address issuer,
        uint256 issuedAt
    );

    function storeCredential(
        string memory credentialId,
        string memory studentCode,
        string memory certificateHash
    ) public {
        require(bytes(credentials[credentialId].credentialId).length == 0, "Credential already exists");
        
        credentials[credentialId] = Credential({
            credentialId: credentialId,
            studentCode: studentCode,
            certificateHash: certificateHash,
            issuer: msg.sender,
            issuedAt: block.timestamp,
            isRevoked: false
        });

        emit CredentialStored(credentialId, studentCode, certificateHash, msg.sender, block.timestamp);
    }

    function getCredential(string memory credentialId) public view returns (Credential memory) {
        return credentials[credentialId];
    }

    function revokeCredential(string memory credentialId) public {
        require(credentials[credentialId].issuer == msg.sender, "Only issuer can revoke");
        credentials[credentialId].isRevoked = true;
    }
}
```

## ?? Security Best Practices

1. **Private Key Management**:
   - KHÔNG commit private key vào Git
   - S? d?ng Azure Key Vault, AWS Secrets Manager, ho?c .env files
   - Trong production, s? d?ng Hardware Security Module (HSM)

2. **Smart Contract Security**:
   - Audit smart contracts tr??c khi deploy lên mainnet
   - S? d?ng OpenZeppelin libraries
   - Implement access control (Ownable, AccessControl)
   - Test k? v?i Hardhat tests

3. **Gas Optimization**:
   - Estimate gas tr??c khi send transaction
   - Set reasonable gas limit và gas price
   - Monitor gas costs

4. **Error Handling**:
   - Luôn wrap blockchain calls trong try-catch
   - Log errors chi ti?t
   - Implement retry logic cho failed transactions

## ?? Tài li?u tham kh?o

- [Nethereum Documentation](https://docs.nethereum.com/)
- [Hardhat Documentation](https://hardhat.org/docs)
- [Ethereum Development](https://ethereum.org/en/developers/docs/)
- [Solidity Documentation](https://docs.soliditylang.org/)

## ?? Troubleshooting

### **L?i: "Contract address is not configured"**
- Ki?m tra `appsettings.json` ?ã có contract address ch?a
- ??m b?o smart contract ?ã ???c deploy

### **L?i: "Blockchain connection failed"**
- Ki?m tra Hardhat node ?ang ch?y (`http://127.0.0.1:8545`)
- Ki?m tra NetworkUrl trong config
- Test connection: `curl http://127.0.0.1:8545`

### **L?i: "Transaction failed"**
- Ki?m tra gas limit ?? l?n
- Ki?m tra account có ?? ETH
- Xem transaction receipt ?? bi?t l?i chi ti?t

### **L?i: "Invalid private key"**
- ??m b?o private key b?t ??u b?ng `0x`
- Private key ph?i là 64 ký t? hex (không tính `0x`)
- S? d?ng private key t? Hardhat test accounts

---

**Created by**: FAP Development Team  
**Last Updated**: January 2025
