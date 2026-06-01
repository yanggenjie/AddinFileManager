param(
    [string]$Changelog,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$slnPath = Join-Path $scriptRoot "source\AddinFileManager.slnx"
$assemblyInfoPath = Join-Path $scriptRoot "source\AddinFileManager\Properties\AssemblyInfo.cs"
$publishDir = Join-Path $scriptRoot "source\bin\AddinFileManager\net472\publish"
$repoOwner = "yanggenjie"
$repoName = "AddinFileManager"

# Read version from AssemblyInfo.cs
$content = Get-Content $assemblyInfoPath -Raw
$regex = '\[assembly:\s*AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]'
if ($content -match $regex) {
    $vMajor = [int]$matches[1]
    $vMinor = [int]$matches[2]
    $vBuild = [int]$matches[3]
    $vRev = [int]$matches[4]

    $oldVersion = "$vMajor.$vMinor.$vBuild.$vRev"
    Write-Host "Current Version: $oldVersion" -ForegroundColor Cyan

    # Auto increment build number and reset revision
    $vBuild++
    $vRev = 0

    $newVersion = "$vMajor.$vMinor.$vBuild.$vRev"
    Write-Host "New Version:     $newVersion" -ForegroundColor Green

    # Update version in AssemblyInfo.cs
    $content = $content -replace '\[assembly:\s*AssemblyVersion\(".*?"\)\]', "[assembly: AssemblyVersion(`"$newVersion`")]"
    $content = $content -replace '\[assembly:\s*AssemblyFileVersion\(".*?"\)\]', "[assembly: AssemblyFileVersion(`"$newVersion`")]"
    Set-Content -Path $assemblyInfoPath -Value $content -NoNewline

    $version = $newVersion
} else {
    Write-Error "Could not parse version from AssemblyInfo.cs"
    exit 1
}

# Check for GitHub token
$githubToken = $env:GITHUB_TOKEN
if ([string]::IsNullOrWhiteSpace($githubToken)) {
    Write-Host "GITHUB_TOKEN environment variable is not set." -ForegroundColor Yellow
    Write-Host "Please set it with: `$env:GITHUB_TOKEN = 'your_token'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To create a token:" -ForegroundColor Cyan
    Write-Host "1. Go to https://github.com/settings/tokens" -ForegroundColor Cyan
    Write-Host "2. Click 'Generate new token (classic)'" -ForegroundColor Cyan
    Write-Host "3. Select 'repo' scope" -ForegroundColor Cyan
    Write-Host "4. Copy the token and set it with the command above" -ForegroundColor Cyan
    exit 1
}

# Build if not skipped
if (-not $SkipBuild) {
    Write-Host "Building Release..." -ForegroundColor Cyan
    dotnet publish $slnPath -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed."
        exit $LASTEXITCODE
    }
}

# Find the exe file
$exePath = Join-Path $publishDir "AddinFileManager.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Cannot find $exePath"
    exit 1
}

$exeInfo = Get-Item $exePath
Write-Host "Release file: $($exeInfo.FullName) ($([math]::Round($exeInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green

# Get changelog
$changelogBody = ""
if ([string]::IsNullOrWhiteSpace($Changelog)) {
    $changelogPath = Join-Path $scriptRoot "CHANGELOG.md"
    if (Test-Path $changelogPath) {
        $changelogContent = Get-Content $changelogPath -Raw
        # Extract section for current version
        if ($changelogContent -match "(?s)##\s+\[$version\](.*?)(?=##\s+\[|$)") {
            $changelogBody = $matches[1].Trim()
        } else {
            $changelogBody = "Version $version release"
        }
    } else {
        Write-Host "No changelog file found. Please provide changelog:" -ForegroundColor Yellow
        $changelogBody = Read-Host "Changelog (press Enter to skip)"
    }
} else {
    $changelogBody = $Changelog
}

# Try to get existing release, create or update
$existingRelease = $null
try {
    $existingRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/$repoOwner/$repoName/releases/tags/v$version" -Headers @{
        "Authorization" = "token $githubToken"
        "Accept" = "application/vnd.github.v3+json"
    } -ErrorAction Stop
} catch {
    # Not found, will create new
}

if ($existingRelease) {
    Write-Host "Release v$version exists, updating..." -ForegroundColor Yellow
    $releaseId = $existingRelease.id
    $releaseUrl = "https://api.github.com/repos/$repoOwner/$repoName/releases/$releaseId"

    $releaseBody = @{
        body = $changelogBody
    } | ConvertTo-Json

    $releaseResponse = Invoke-RestMethod -Uri $releaseUrl -Method PATCH -Headers @{
        "Authorization" = "token $githubToken"
        "Accept" = "application/vnd.github.v3+json"
        "Content-Type" = "application/json"
    } -Body $releaseBody

    Write-Host "Release updated: $($releaseResponse.html_url)" -ForegroundColor Green
} else {
    Write-Host "Creating release v$version..." -ForegroundColor Cyan

    $releaseUrl = "https://api.github.com/repos/$repoOwner/$repoName/releases"

    $releaseBody = @{
        tag_name = "v$version"
        name = "v$version"
        body = $changelogBody
        draft = $false
        prerelease = $false
    } | ConvertTo-Json

    $releaseResponse = Invoke-RestMethod -Uri $releaseUrl -Method POST -Headers @{
        "Authorization" = "token $githubToken"
        "Accept" = "application/vnd.github.v3+json"
        "Content-Type" = "application/json"
    } -Body $releaseBody

    Write-Host "Release created: $($releaseResponse.html_url)" -ForegroundColor Green
}

# Upload asset
$uploadUrlBase = $releaseResponse.upload_url.Split('{')[0]
$fileName = "AddinFileManager.exe"

Write-Host "Uploading $fileName..." -ForegroundColor Cyan

# Read file as binary
$fileBytes = [System.IO.File]::ReadAllBytes($exePath)

$assetResponse = Invoke-RestMethod -Uri "${uploadUrlBase}?name=$fileName" -Method POST -Headers @{
    "Authorization" = "token $githubToken"
    "Accept" = "application/vnd.github.v3+json"
    "Content-Type" = "application/octet-stream"
} -Body $fileBytes

Write-Host "Upload complete!" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "GitHub Release completed!" -ForegroundColor Green
Write-Host "Version: v$version" -ForegroundColor Green
Write-Host "URL: $($releaseResponse.html_url)" -ForegroundColor Cyan