# ============================================
# RESET AZURE DATABASE VIA API
# ============================================
# This script resets the Azure database by calling the Database Admin API
# ?? WARNING: This will DELETE ALL DATA in the database!

param(
 [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://your-azure-app.azurewebsites.net",
    
    [Parameter(Mandatory=$false)]
 [string]$Action = "reset"  # Options: reset, migrate, reseed, status
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "AZURE DATABASE ADMIN TOOL" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Prompt for Azure API URL if not provided
if ($ApiUrl -eq "https://your-azure-app.azurewebsites.net") {
    $ApiUrl = Read-Host "Enter your Azure API URL (e.g., https://fap-api.azurewebsites.net)"
}

$baseUrl = $ApiUrl.TrimEnd('/')
$endpoint = "$baseUrl/api/DatabaseAdmin/$Action"

Write-Host "Target: $endpoint" -ForegroundColor Yellow
Write-Host ""

# Handle different actions
switch ($Action) {
 "reset" {
        Write-Host "??  WARNING: This will DELETE ALL DATA in the database!" -ForegroundColor Red
        Write-Host "??  All tables will be dropped and recreated with fresh seed data!" -ForegroundColor Red
        Write-Host ""
 $confirmation = Read-Host "Type 'YES' to confirm database reset"
        
        if ($confirmation -ne "YES") {
            Write-Host "? Database reset cancelled" -ForegroundColor Yellow
            exit
        }
 
        Write-Host ""
        Write-Host "???  Resetting database..." -ForegroundColor Yellow
    
    try {
            $response = Invoke-RestMethod -Uri "$endpoint`?confirmationToken=CONFIRM_RESET_DATABASE" `
            -Method Post `
           -ContentType "application/json" `
                -ErrorAction Stop
       
            Write-Host "? Database reset successful!" -ForegroundColor Green
Write-Host ""
  Write-Host "Response:" -ForegroundColor Cyan
            $response | ConvertTo-Json -Depth 5
        }
     catch {
            Write-Host "? Database reset failed!" -ForegroundColor Red
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
     
     if ($_.Exception.Response) {
     $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
         Write-Host "Response: $responseBody" -ForegroundColor Red
       }
 }
    }
    
    "migrate" {
        Write-Host "?? Applying pending migrations..." -ForegroundColor Yellow
        
     try {
         $response = Invoke-RestMethod -Uri $endpoint `
                -Method Post `
              -ContentType "application/json" `
           -ErrorAction Stop

            Write-Host "? Migrations applied successfully!" -ForegroundColor Green
    Write-Host ""
            $response | ConvertTo-Json -Depth 5
        }
     catch {
            Write-Host "? Migration failed!" -ForegroundColor Red
   Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
 }
    }
    
    "reseed" {
        Write-Host "??  WARNING: This may create duplicate data!" -ForegroundColor Yellow
        Write-Host ""
     $confirmation = Read-Host "Type 'YES' to confirm data reseeding"
        
 if ($confirmation -ne "YES") {
            Write-Host "? Reseeding cancelled" -ForegroundColor Yellow
            exit
        }
        
        Write-Host ""
      Write-Host "?? Reseeding data..." -ForegroundColor Yellow
        
        try {
            $response = Invoke-RestMethod -Uri $endpoint `
  -Method Post `
         -ContentType "application/json" `
      -ErrorAction Stop
    
   Write-Host "? Data reseeded successfully!" -ForegroundColor Green
   Write-Host ""
        $response | ConvertTo-Json -Depth 5
      }
      catch {
   Write-Host "? Reseeding failed!" -ForegroundColor Red
      Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    "status" {
        Write-Host "?? Checking database status..." -ForegroundColor Yellow
        Write-Host ""
 
        try {
            $response = Invoke-RestMethod -Uri $endpoint `
             -Method Get `
    -ContentType "application/json" `
 -ErrorAction Stop
   
        Write-Host "? Database status retrieved!" -ForegroundColor Green
        Write-Host ""
     Write-Host "Connection: " -NoNewline
            if ($response.canConnect) {
                Write-Host "? Connected" -ForegroundColor Green
            } else {
       Write-Host "? Not Connected" -ForegroundColor Red
   }
            
     Write-Host ""
            Write-Host "Applied Migrations: $($response.appliedMigrations.Count)" -ForegroundColor Cyan
       Write-Host "Pending Migrations: $($response.pendingMigrations.Count)" -ForegroundColor Yellow
          
            if ($response.hasPendingMigrations) {
            Write-Host ""
    Write-Host "??  Pending migrations found:" -ForegroundColor Yellow
 $response.pendingMigrations | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
          }
            
  Write-Host ""
    Write-Host "Settings:" -ForegroundColor Cyan
          Write-Host "  Auto Reset On Startup: $($response.settings.autoResetOnStartup)" -ForegroundColor White
     Write-Host "  Database Admin API Enabled: $($response.settings.allowDatabaseAdminApi)" -ForegroundColor White
        }
        catch {
    Write-Host "? Failed to get database status!" -ForegroundColor Red
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
     }
    }
    
    default {
     Write-Host "? Invalid action: $Action" -ForegroundColor Red
 Write-Host "Valid actions: reset, migrate, reseed, status" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "DONE" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
