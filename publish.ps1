$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$slnPath = Join-Path $scriptRoot "source\AddinFileManager.slnx"
$assemblyInfoPath = Join-Path $scriptRoot "source\AddinFileManager\Properties\AssemblyInfo.cs"
$publishDir = Join-Path $scriptRoot "source\bin\AddinFileManager\net472\publish"

if (-Not (Test-Path $assemblyInfoPath)) {
    Write-Error "Cannot find AssemblyInfo.cs at $assemblyInfoPath"
    exit 1
}
if (-Not (Test-Path $slnPath)) {
    Write-Error "Cannot find solution at $slnPath"
    exit 1
}

$content = Get-Content $assemblyInfoPath -Raw

# Match current version
$regex = '\[assembly:\s*AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]'
if ($content -match $regex) {
    $vMajor = [int]$matches[1]
    $vMinor = [int]$matches[2]
    $vBuild = [int]$matches[3]
    $vRev   = [int]$matches[4]

    $oldVersion = "$vMajor.$vMinor.$vBuild.$vRev"
    Write-Host "Current Version: $oldVersion" -ForegroundColor Cyan

    # 默认更新第三个版本号 (Build)，并将第四位清零
    $vBuild++
    $vRev = 0

    $newVersion = "$vMajor.$vMinor.$vBuild.$vRev"
    Write-Host "New Version:     $newVersion" -ForegroundColor Green

    # Replace version strings in AssemblyInfo.cs
    $content = $content -replace '\[assembly:\s*AssemblyVersion\(".*?"\)\]', "[assembly: AssemblyVersion(`"$newVersion`")]"
    $content = $content -replace '\[assembly:\s*AssemblyFileVersion\(".*?"\)\]', "[assembly: AssemblyFileVersion(`"$newVersion`")]"

    Set-Content -Path $assemblyInfoPath -Value $content -NoNewline
    
    # Run clean + publish in Release mode
    Write-Host "Cleaning Release..." -ForegroundColor Cyan
    dotnet clean $slnPath -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet clean failed."
        exit $LASTEXITCODE
    }

    Write-Host "Publishing Release..." -ForegroundColor Cyan
    dotnet publish $slnPath -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed."
        exit $LASTEXITCODE
    }

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Release publish completed successfully!" -ForegroundColor Green
    Write-Host "Version: $newVersion" -ForegroundColor Green
    
    # Check publish result
    $exePath = Join-Path $publishDir "AddinFileManager.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        $releaseTime = $fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "Release Time: $releaseTime" -ForegroundColor Green
        Write-Host "Publish path: $publishDir" -ForegroundColor Yellow

        $dllFiles = Get-ChildItem -Path $publishDir -Filter *.dll -File -Recurse
        if ($dllFiles.Count -gt 0) {
            Write-Host "Unexpected DLL files found in publish output:" -ForegroundColor Red
            $dllFiles | ForEach-Object { Write-Host " - $($_.FullName)" -ForegroundColor Red }
            Write-Error "Publish validation failed: DLL files are still present."
            exit 1
        }

        Write-Host "Publish validation passed: no DLL files in output." -ForegroundColor Green
    } else {
        Write-Error "Publish failed: cannot find $exePath"
        exit 1
    }
} else {
    Write-Error "Could not parse version from AssemblyInfo.cs"
}
