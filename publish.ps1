$ErrorActionPreference = "Stop"

$slnPath = "source\AddinFileManager.slnx"
$assemblyInfoPath = "source\AddinFileManager\Properties\AssemblyInfo.cs"
$outDir = "source\bin\AddinFileManager\net472"

if (-Not (Test-Path $assemblyInfoPath)) {
    Write-Error "Cannot find AssemblyInfo.cs at $assemblyInfoPath"
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
    
    # Run the build in Release mode
    Write-Host "Building Release..." -ForegroundColor Cyan
    dotnet build $slnPath -c Release

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Release build completed successfully!" -ForegroundColor Green
    Write-Host "Version: $newVersion" -ForegroundColor Green
    
    # Check if exe exists and display its time
    $exePath = Join-Path $outDir "AddinFileManager.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        $releaseTime = $fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "Release Time: $releaseTime" -ForegroundColor Green
        Write-Host "Output path: $exePath" -ForegroundColor Yellow
    }
} else {
    Write-Error "Could not parse version from AssemblyInfo.cs"
}
