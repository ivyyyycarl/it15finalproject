param(
    [int]$Port = 5300
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "INTEGRATE SUPPORT SALES MANAGEMENT SYSTEM.csproj"

if (-not (Test-Path $projectFile)) {
    throw "Project file not found at '$projectFile'."
}

$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    $pidToStop = $listener.OwningProcess
    Write-Host "Stopping existing process on port $Port (PID: $pidToStop)..." -ForegroundColor Yellow
    Stop-Process -Id $pidToStop -Force -ErrorAction SilentlyContinue
}

Write-Host "Starting app at http://localhost:$Port ..." -ForegroundColor Cyan
dotnet run --project "$projectFile" --urls "http://localhost:$Port"
