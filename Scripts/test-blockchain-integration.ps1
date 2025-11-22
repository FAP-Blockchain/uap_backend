# ============================================================
# Test Blockchain Integration Script
# ============================================================
# This script helps test the blockchain credential management integration
# 
# Prerequisites:
# 1. Hardhat node running (npx hardhat node)
# 2. Smart contract deployed (npx hardhat run scripts/deploy.js --network localhost)
# 3. API running (dotnet run --project Fap.Api)
#
# ============================================================

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$BlockchainUrl = "http://127.0.0.1:8545",
    [switch]$SkipBlockchainCheck
)

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  Blockchain Integration Test" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check Blockchain Node
if (-not $SkipBlockchainCheck) {
    Write-Host "📡 Step 1: Checking Blockchain Node..." -ForegroundColor Yellow
    try {
        $body = @{
            jsonrpc = "2.0"
            method = "eth_blockNumber"
            params = @()
            id = 1
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri $BlockchainUrl -Method Post -ContentType "application/json" -Body $body
        $blockNumber = [Convert]::ToInt64($response.result, 16)
        Write-Host "   ✅ Blockchain node is running (Block: $blockNumber)" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ Blockchain node is NOT running!" -ForegroundColor Red
        Write-Host "   💡 Start Hardhat node: npx hardhat node" -ForegroundColor Yellow
        exit 1
    }
}

# Step 2: Check API Health
Write-Host ""
Write-Host "🏥 Step 2: Checking API Health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$ApiUrl/health" -Method Get
    Write-Host "   ✅ API is running" -ForegroundColor Green
}
catch {
    Write-Host "   ❌ API is NOT running!" -ForegroundColor Red
    Write-Host "   💡 Start API: dotnet run --project Fap.Api" -ForegroundColor Yellow
    exit 1
}

# Step 3: Login to get token
Write-Host ""
Write-Host "🔑 Step 3: Getting authentication token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "admin@fap.edu.vn"
        password = "Admin@123"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token = $loginResponse.accessToken
    Write-Host "   ✅ Logged in as admin" -ForegroundColor Green
}
catch {
    Write-Host "   ❌ Failed to login!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 4: Get existing credentials
Write-Host ""
Write-Host "📜 Step 4: Getting existing credentials..." -ForegroundColor Yellow
try {
    $credentials = Invoke-RestMethod -Uri "$ApiUrl/api/credentials" -Method Get -Headers $headers
    $totalCreds = $credentials.data.Count
    Write-Host "   ✅ Found $totalCreds credentials" -ForegroundColor Green
    
    # Show credentials with blockchain data
    $withBlockchain = $credentials.data | Where-Object { $_.blockchainCredentialId -ne $null }
    Write-Host "   📊 Credentials on blockchain: $($withBlockchain.Count)" -ForegroundColor Cyan
    
    if ($withBlockchain.Count -gt 0) {
        Write-Host ""
        Write-Host "   Blockchain Credentials:" -ForegroundColor Cyan
        $withBlockchain | ForEach-Object {
            Write-Host "      • $($_.credentialId) - Blockchain ID: $($_.blockchainCredentialId) - TxHash: $($_.blockchainTransactionHash)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "   ⚠️ Warning: Could not fetch credentials" -ForegroundColor Yellow
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Test Blockchain Service
Write-Host ""
Write-Host "🔗 Step 5: Testing Blockchain Service..." -ForegroundColor Yellow
Write-Host "   This will test the blockchain credential verification" -ForegroundColor Gray

# Get a credential with blockchain data
if ($withBlockchain.Count -gt 0) {
    $testCred = $withBlockchain[0]
    Write-Host ""
    Write-Host "   Testing credential: $($testCred.credentialId)" -ForegroundColor Cyan
    Write-Host "      Blockchain ID: $($testCred.blockchainCredentialId)" -ForegroundColor Gray
    Write-Host "      TxHash: $($testCred.blockchainTransactionHash)" -ForegroundColor Gray
    
    # Test public verification endpoint
    try {
        $publicCred = Invoke-RestMethod -Uri "$ApiUrl/api/credentials/public/$($testCred.credentialId)" -Method Get
        Write-Host "   ✅ Public verification successful" -ForegroundColor Green
        
        if ($publicCred.isOnBlockchain) {
            Write-Host "      ✓ On blockchain: Yes" -ForegroundColor Green
            Write-Host "      ✓ Blockchain ID: $($publicCred.blockchainCredentialId)" -ForegroundColor Green
            Write-Host "      ✓ Transaction Hash: $($publicCred.blockchainTransactionHash)" -ForegroundColor Green
        }
        else {
            Write-Host "      ⚠️ Not on blockchain yet" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "   ❌ Public verification failed" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
else {
    Write-Host "   ⚠️ No credentials with blockchain data found" -ForegroundColor Yellow
    Write-Host "   💡 Run the seeder to create test credentials" -ForegroundColor Yellow
}

# Step 6: Summary
Write-Host ""
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "✅ Blockchain node: Connected" -ForegroundColor Green
Write-Host "✅ API: Running" -ForegroundColor Green
Write-Host "✅ Authentication: Working" -ForegroundColor Green
Write-Host "📊 Total credentials: $totalCreds" -ForegroundColor Cyan
Write-Host "🔗 Blockchain credentials: $($withBlockchain.Count)" -ForegroundColor Cyan
Write-Host ""

# Next steps
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  Next Steps for Full Integration Test" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Deploy Smart Contract (if not done):" -ForegroundColor Yellow
Write-Host "   cd smart_contracts" -ForegroundColor Gray
Write-Host "   npx hardhat run scripts/deploy.js --network localhost" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Update appsettings.json with contract address" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Test credential issuance on blockchain:" -ForegroundColor Yellow
Write-Host "   POST $ApiUrl/api/credentials" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Verify credential on blockchain:" -ForegroundColor Yellow
Write-Host "   GET $ApiUrl/api/credentials/verify/{credentialId}" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Test revocation on blockchain:" -ForegroundColor Yellow
Write-Host "   PUT $ApiUrl/api/credentials/{id}/revoke" -ForegroundColor Gray
Write-Host ""
