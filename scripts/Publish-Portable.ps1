param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

function Get-ProjectProperty {
    param(
        [xml]$ProjectXml,
        [string]$PropertyName
    )

    foreach ($group in $ProjectXml.Project.PropertyGroup) {
        $value = $group.$PropertyName
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
    }

    throw "Project property '$PropertyName' was not found."
}

function Get-AppMetadataValue {
    param(
        [string]$FilePath,
        [string]$ConstantName
    )

    $content = Get-Content -Path $FilePath -Raw
    $pattern = [regex]::Escape($ConstantName) + '\s*=\s*"([^"]+)"'
    $match = [regex]::Match($content, $pattern)

    if (-not $match.Success) {
        throw "AppMetadata constant '$ConstantName' was not found."
    }

    return $match.Groups[1].Value
}

function Write-ReleaseNotes {
    param(
        [string]$OutputPath,
        [string]$ProductName,
        [string]$Version,
        [string]$Runtime,
        [string]$PackageMode,
        [string]$StandardVersion,
        [string]$BuildTime
    )

    $lines = @(
        "$ProductName",
        "Version: $Version",
        "Package: Portable self-contained ($PackageMode)",
        "Runtime target: $Runtime",
        "Build time: $BuildTime",
        "Calculation basis: $StandardVersion",
        "",
        "This package includes the required .NET runtime.",
        "The target PC does not need a separate .NET installation.",
        "",
        "How to use:",
        "1. Extract the entire folder to any local directory.",
        "2. Run PSVCalc.App.exe.",
        "3. Project, history, export, and validation files are stored under Documents\\PSVCalc.",
        "",
        "Current scope highlights:",
        "- Gas, steam, liquid, and selected two-phase sizing workflows",
        "- Fire, overpressure, tube rupture, and thermal expansion scenarios",
        "- API orifice recommendation plus direct HG/T throat-diameter presentation",
        "- Project save/load, history, Excel export, and validation report export",
        "",
        "Notes:",
        "- Portable package only. No installer is included.",
        "- Intended for Windows x64 systems."
    )

    Set-Content -Path $OutputPath -Value $lines -Encoding UTF8
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\PSVCalc.App\PSVCalc.App.csproj"
$metadataPath = Join-Path $repoRoot "src\PSVCalc.Core\AppMetadata.cs"
$changeLogPath = Join-Path $repoRoot "CHANGELOG.md"
$portableRoot = Join-Path $repoRoot "publish\portable-selfcontained"
$archiveRoot = Join-Path $portableRoot "archives"

[xml]$projectXml = Get-Content -Path $projectPath -Raw
$productName = Get-ProjectProperty -ProjectXml $projectXml -PropertyName "Product"
$version = Get-ProjectProperty -ProjectXml $projectXml -PropertyName "Version"
$standardVersion = Get-AppMetadataValue -FilePath $metadataPath -ConstantName "StandardVersion"
$buildTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$packageBaseName = "PSV-Calculator-Pro-V$version-portable-$Runtime"
$singlePackageName = "$packageBaseName-singlefile"
$multiPackageName = "$packageBaseName-multifile"

$singleFileOutput = Join-Path $portableRoot $singlePackageName
$multiFileOutput = Join-Path $portableRoot $multiPackageName
$singleFileZip = Join-Path $archiveRoot "$singlePackageName.zip"
$multiFileZip = Join-Path $archiveRoot "$multiPackageName.zip"

New-Item -ItemType Directory -Path $portableRoot -Force | Out-Null
New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null

foreach ($path in @($singleFileOutput, $multiFileOutput, $singleFileZip, $multiFileZip)) {
    if (Test-Path $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $singleFileOutput

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o $multiFileOutput

Write-ReleaseNotes `
    -OutputPath (Join-Path $singleFileOutput "Release-Notes.txt") `
    -ProductName $productName `
    -Version $version `
    -Runtime $Runtime `
    -PackageMode "single-file" `
    -StandardVersion $standardVersion `
    -BuildTime $buildTime

Write-ReleaseNotes `
    -OutputPath (Join-Path $multiFileOutput "Release-Notes.txt") `
    -ProductName $productName `
    -Version $version `
    -Runtime $Runtime `
    -PackageMode "multi-file" `
    -StandardVersion $standardVersion `
    -BuildTime $buildTime

Copy-Item -LiteralPath $changeLogPath -Destination (Join-Path $singleFileOutput "CHANGELOG.md") -Force
Copy-Item -LiteralPath $changeLogPath -Destination (Join-Path $multiFileOutput "CHANGELOG.md") -Force

Compress-Archive -Path $singleFileOutput -DestinationPath $singleFileZip -Force
Compress-Archive -Path $multiFileOutput -DestinationPath $multiFileZip -Force

Write-Host "Portable single-file output: $singleFileOutput"
Write-Host "Portable multi-file output:  $multiFileOutput"
Write-Host "Portable single-file zip:    $singleFileZip"
Write-Host "Portable multi-file zip:     $multiFileZip"
