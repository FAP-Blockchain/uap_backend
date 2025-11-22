# ========================================
# Re-seed Credentials for Testing
# ========================================
# This script removes existing credentials and re-seeds with test data

Write-Host "🔄 Re-seeding Credentials Test Data..." -ForegroundColor Cyan
Write-Host ""

$projectPath = "E:\DoAnRaTruong\fap_blockchain_backend\Fap.Api"

# Navigate to project directory
Push-Location $projectPath

try {
    Write-Host "📋 Step 1: Removing existing credentials..." -ForegroundColor Yellow
    
    # Create a temporary migration to delete credentials
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    
    # You can either:
    # Option A: Run SQL directly (requires connection string)
    # Option B: Drop and recreate database (if using local dev)
    # Option C: Manually delete from database
    
    Write-Host "   ⚠️  Please choose an option:" -ForegroundColor Yellow
    Write-Host "   1. Drop and recreate database (Local Dev only)" -ForegroundColor White
    Write-Host "   2. Delete credentials via SQL script" -ForegroundColor White
    Write-Host "   3. Cancel" -ForegroundColor White
    
    $choice = Read-Host "   Enter choice (1-3)"
    
    switch ($choice) {
        "1" {
            Write-Host ""
            Write-Host "📦 Dropping database..." -ForegroundColor Yellow
            dotnet ef database drop --force --project ..\Fap.Infrastructure
            
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to drop database"
            }
            
            Write-Host "   ✅ Database dropped" -ForegroundColor Green
            Write-Host ""
            Write-Host "📦 Recreating database with migrations..." -ForegroundColor Yellow
            dotnet ef database update --project ..\Fap.Infrastructure
            
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to recreate database"
            }
            
            Write-Host "   ✅ Database recreated" -ForegroundColor Green
            Write-Host ""
            Write-Host "🌱 Seeding data..." -ForegroundColor Yellow
            Write-Host "   Starting application to trigger seeding..." -ForegroundColor Gray
            Write-Host "   (Application will seed data on startup)" -ForegroundColor Gray
            Write-Host ""
            Write-Host "   Run: dotnet run --project $projectPath" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "   Or use Docker:" -ForegroundColor Cyan
            Write-Host "   docker-compose up --build" -ForegroundColor Cyan
        }
        
        "2" {
            Write-Host ""
            Write-Host "📝 Creating SQL cleanup script..." -ForegroundColor Yellow
            
            $sqlScript = @"
-- Delete credentials in correct order to avoid FK constraints
BEGIN TRANSACTION;

DELETE FROM ActionLogs WHERE CredentialId IS NOT NULL;
DELETE FROM Credentials;
DELETE FROM CredentialRequests;

COMMIT;

-- Reset identity seeds (if using IDENTITY columns)
-- DBCC CHECKIDENT ('Credentials', RESEED, 0);
"@
            
            $sqlFile = ".\cleanup-credentials.sql"
            $sqlScript | Out-File -FilePath $sqlFile -Encoding UTF8
            
            Write-Host "   ✅ SQL script created: $sqlFile" -ForegroundColor Green
            Write-Host ""
            Write-Host "   Execute this script against your database, then restart the app to reseed." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "   Using SQL Server Management Studio:" -ForegroundColor Cyan
            Write-Host "   1. Open SSMS and connect to your database" -ForegroundColor White
            Write-Host "   2. Open the file: $sqlFile" -ForegroundColor White
            Write-Host "   3. Execute the script" -ForegroundColor White
            Write-Host "   4. Restart the application: dotnet run" -ForegroundColor White
            Write-Host ""
            Write-Host "   Or using sqlcmd:" -ForegroundColor Cyan
            Write-Host "   sqlcmd -S localhost -d FapDb -i $sqlFile" -ForegroundColor White
        }
        
        "3" {
            Write-Host ""
            Write-Host "❌ Cancelled" -ForegroundColor Red
            exit 0
        }
        
        default {
            Write-Host ""
            Write-Host "❌ Invalid choice" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host ""
    Write-Host "✅ Done!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 After reseeding, you will have:" -ForegroundColor Cyan
    Write-Host "   • 9 test credentials with different statuses" -ForegroundColor White
    Write-Host "   • Case 1: Issued + Blockchain + QR Code" -ForegroundColor White
    Write-Host "   • Case 2: Issued WITHOUT QR (lazy generation test)" -ForegroundColor White
    Write-Host "   • Case 3: Revoked certificate" -ForegroundColor White
    Write-Host "   • Case 4: Semester completion" -ForegroundColor White
    Write-Host "   • Case 5: High view count (150 views)" -ForegroundColor White
    Write-Host "   • Case 6: Graduation certificate" -ForegroundColor White
    Write-Host "   • Case 7: Pending credential" -ForegroundColor White
    Write-Host "   • Case 8: Recently issued (no views)" -ForegroundColor White
    Write-Host "   • Case 9: Blockchain pending" -ForegroundColor White
    Write-Host ""
    Write-Host "🧪 Test Scenarios:" -ForegroundColor Cyan
    Write-Host "   1. GET /api/credentials/public/{id} - Test public view" -ForegroundColor White
    Write-Host "   2. GET /api/students/me/certificates/share - Test student share list" -ForegroundColor White
    Write-Host "   3. Check QR code lazy generation (Case 2)" -ForegroundColor White
    Write-Host "   4. Verify blockchain status (Case 9)" -ForegroundColor White
    Write-Host "   5. Test revoked certificate behavior (Case 3)" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
