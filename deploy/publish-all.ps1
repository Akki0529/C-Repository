# Builds a Release, self-contained-free publish output for each of the three
# services and zips each one — the format AWS Elastic Beanstalk's .NET platform
# expects for a manual "upload and deploy" application version.
#
# This script only touches the local filesystem: it does not require AWS
# credentials and does not talk to AWS in any way. Uploading the resulting
# .zip files to Elastic Beanstalk is a separate, deliberate step (console or
# `aws elasticbeanstalk create-application-version` / eb cli), left to you.
#
# Usage (from the repository root):
#   ./deploy/publish-all.ps1
#   ./deploy/publish-all.ps1 -OutputRoot ./publish -Configuration Release

param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "./publish"
)

$ErrorActionPreference = "Stop"
$services = @("UserService", "CatalogService", "ReservationService")

if (Test-Path $OutputRoot) {
    Remove-Item $OutputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

foreach ($service in $services) {
    $projectPath = Join-Path $service "$service.csproj"
    $publishDir = Join-Path $OutputRoot $service

    Write-Host "==> Publishing $service ($Configuration)..." -ForegroundColor Cyan
    dotnet publish $projectPath -c $Configuration -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $service (exit code $LASTEXITCODE)"
    }

    $zipPath = Join-Path $OutputRoot "$service.zip"
    Write-Host "==> Zipping $service -> $zipPath" -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "Done. Deployable artifacts:" -ForegroundColor Green
foreach ($service in $services) {
    Write-Host "  $(Join-Path $OutputRoot "$service.zip")"
}
Write-Host ""
Write-Host "Next: upload each zip as a new Elastic Beanstalk application version" -ForegroundColor Yellow
Write-Host "for that service's environment, with its environment variables already" -ForegroundColor Yellow
Write-Host "configured per docs/milestone-5-deployment-production-readiness.md." -ForegroundColor Yellow
