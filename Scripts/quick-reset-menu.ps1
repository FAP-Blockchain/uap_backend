# ============================================
# QUICK RESET YOUR AZURE DATABASE
# ============================================
# Simple wrapper script for reset-azure-database.ps1

# ?? C?U HÌNH - Thay ??i URL này thành Azure App URL c?a b?n
$AZURE_API_URL = "https://your-app-name.azurewebsites.net"

# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "AZURE DATABASE RESET TOOL" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Azure API: $AZURE_API_URL" -ForegroundColor Yellow
Write-Host ""
Write-Host "Select an action:" -ForegroundColor White
Write-Host "  [1] Reset Database (?? DELETE ALL DATA)" -ForegroundColor Red
Write-Host "  [2] Apply Migrations Only" -ForegroundColor Yellow
Write-Host "  [3] Reseed Data (may duplicate)" -ForegroundColor Yellow
Write-Host "  [4] Check Database Status" -ForegroundColor Green
Write-Host "  [5] Exit" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter your choice (1-5)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "??  WARNING: This will DELETE ALL DATA!" -ForegroundColor Red
  $confirm = Read-Host "Type 'YES' to confirm"
        if ($confirm -eq "YES") {
 & "$PSScriptRoot\reset-azure-database.ps1" -ApiUrl $AZURE_API_URL -Action "reset"
        } else {
            Write-Host "? Cancelled" -ForegroundColor Yellow
        }
    }
    "2" {
        & "$PSScriptRoot\reset-azure-database.ps1" -ApiUrl $AZURE_API_URL -Action "migrate"
    }
    "3" {
        Write-Host ""
        Write-Host "??  This may create duplicate data!" -ForegroundColor Yellow
     $confirm = Read-Host "Type 'YES' to confirm"
     if ($confirm -eq "YES") {
            & "$PSScriptRoot\reset-azure-database.ps1" -ApiUrl $AZURE_API_URL -Action "reseed"
        } else {
            Write-Host "? Cancelled" -ForegroundColor Yellow
        }
    }
    "4" {
        & "$PSScriptRoot\reset-azure-database.ps1" -ApiUrl $AZURE_API_URL -Action "status"
    }
    "5" {
        Write-Host "Goodbye!" -ForegroundColor Green
        exit
    }
    default {
   Write-Host "? Invalid choice" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
