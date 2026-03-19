param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$frontendProject = Join-Path $projectRoot "SupportSalesManagement.Frontend\SupportSalesManagement.Frontend.csproj"
$frontendPublishRoot = Join-Path $projectRoot "SupportSalesManagement.Frontend\bin\$Configuration\net8.0\publish\wwwroot"
$backendWwwroot = Join-Path $projectRoot "wwwroot"

if (-not (Test-Path $frontendProject)) {
    throw "Frontend project was not found at '$frontendProject'."
}

Write-Host "Publishing frontend ($Configuration)..." -ForegroundColor Cyan
dotnet publish "$frontendProject" -c "$Configuration" | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Frontend publish failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $frontendPublishRoot)) {
    throw "Frontend publish output was not found at '$frontendPublishRoot'."
}

Write-Host "Syncing frontend assets to backend wwwroot..." -ForegroundColor Cyan
# Mirror output so removed/renamed frontend files don't remain in backend wwwroot.
robocopy "$frontendPublishRoot" "$backendWwwroot" /MIR /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null

# Robocopy returns 0-7 for success scenarios.
if ($LASTEXITCODE -ge 8) {
    throw "Asset sync failed (robocopy exit code $LASTEXITCODE)."
}

Write-Host "Frontend assets synced successfully." -ForegroundColor Green
