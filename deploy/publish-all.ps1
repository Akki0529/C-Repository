# Builds a single deployment bundle (library-microservices.zip) containing all three
# services, the Procfile, and the nginx platform extension.
#
# All three services run as separate processes on ONE Elastic Beanstalk environment
# (the sandbox allows at most one environment). The Procfile tells EB how to start
# each process; services.conf tells nginx how to route /catalog/ and /reservations/
# to the right port.
#
# Usage (from the repository root):
#   ./deploy/publish-all.ps1
#   ./deploy/publish-all.ps1 -BundleDir ./bundle -Configuration Release

param(
    [string]$Configuration = "Release",
    [string]$BundleDir = "./bundle"
)

$ErrorActionPreference = "Stop"
$services = @("UserService", "CatalogService", "ReservationService")

# Clean and recreate bundle staging directory
if (Test-Path $BundleDir) {
    Remove-Item $BundleDir -Recurse -Force
}
New-Item -ItemType Directory -Path $BundleDir -Force | Out-Null

foreach ($service in $services) {
    $projectPath = Join-Path $service "$service.csproj"
    $publishDir  = Join-Path $BundleDir $service

    Write-Host "==> Publishing $service ($Configuration)..." -ForegroundColor Cyan
    dotnet publish $projectPath -c $Configuration -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $service (exit code $LASTEXITCODE)"
    }
}

Write-Host "==> Copying Procfile and .platform/..." -ForegroundColor Cyan
Copy-Item -Path "Procfile"   -Destination (Join-Path $BundleDir "Procfile")
Copy-Item -Path ".platform"  -Destination (Join-Path $BundleDir ".platform") -Recurse

# Use ZipFile API instead of Compress-Archive: Compress-Archive writes Windows
# backslash separators inside the zip, which Linux's unzip rejects with exit 1.
# ZipFile.CreateEntry writes the path we give it literally, so we replace \ with /.
Add-Type -Assembly System.IO.Compression
Add-Type -Assembly System.IO.Compression.FileSystem
$zipPath = (Join-Path (Get-Location) "library-microservices.zip")
if (Test-Path $zipPath) { [System.IO.File]::Delete($zipPath) }
$abs     = (Resolve-Path $BundleDir).Path
$za      = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
Get-ChildItem $BundleDir -Recurse -Force -File | ForEach-Object {
    $rel   = $_.FullName.Substring($abs.Length + 1).Replace('\', '/')
    $entry = $za.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
    $out   = $entry.Open()
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    $out.Write($bytes, 0, $bytes.Length)
    $out.Dispose()
}
$za.Dispose()

Write-Host ""
Write-Host "Done. Deployable artifact: $zipPath" -ForegroundColor Green
Write-Host ""
Write-Host "Verify the zip shape (Procfile and .platform must be at the root):" -ForegroundColor Yellow
Write-Host "  Get-ChildItem $zipPath | ... (or inspect with 7-Zip)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next: upload $zipPath to your Elastic Beanstalk environment as a new" -ForegroundColor Yellow
Write-Host "application version per docs/production-enviroment-setup.md Part 3."   -ForegroundColor Yellow
