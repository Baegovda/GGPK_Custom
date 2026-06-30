# LibGGPK3 build environment setup (Windows)
$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot

function Ensure-DotNetSdk {
    $sdk = & dotnet --list-sdks 2>$null | Select-String '^8\.'
    if (-not $sdk) {
        Write-Host 'Installing .NET 8 SDK...'
        winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
        $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
    }
    dotnet --version
}

function Ensure-Oo2Core {
    $dest = Join-Path $Root 'LibBundle3\oo2core\oo2core.dll'
    if (Test-Path $dest) { return }

    Write-Host 'Downloading oo2core.dll from LibGGPK3 v2.7.5 release...'
    $tmp = Join-Path $Root '_setup'
    $zip = Join-Path $tmp 'win-x64.zip'
    New-Item -ItemType Directory -Force -Path (Join-Path $tmp 'extract') | Out-Null
    Invoke-WebRequest -Uri 'https://github.com/aianlinb/LibGGPK3/releases/download/v2.7.5/win-x64.zip' -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath (Join-Path $tmp 'extract') -Force
    $src = Get-ChildItem (Join-Path $tmp 'extract') -Recurse -Filter 'oo2core.dll' | Select-Object -First 1
    if (-not $src) { throw 'oo2core.dll not found in release zip' }
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item $src.FullName $dest -Force
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

function Ensure-LibDat2 {
    $proj = Join-Path $Root 'external\VisualGGPK2\LibDat2\LibDat2.csproj'
    if (Test-Path $proj) { return }

    Write-Host 'Cloning LibDat2 source from VisualGGPK2...'
    $external = Join-Path $Root 'external'
    New-Item -ItemType Directory -Force -Path $external | Out-Null
    git clone --depth 1 --filter=blob:none --sparse 'https://github.com/aianlinb/VisualGGPK2.git' (Join-Path $external 'VisualGGPK2')
    Push-Location (Join-Path $external 'VisualGGPK2')
    git sparse-checkout set LibDat2
    Pop-Location

    (Get-Content $proj -Raw) -replace 'net6\.0', 'net8.0' | Set-Content $proj -NoNewline
}

Write-Host '=== LibGGPK3 setup ==='
Ensure-DotNetSdk
Ensure-Oo2Core
Ensure-LibDat2

$sln = Join-Path $Root 'LibGGPK3.sln'
& dotnet sln $sln list | Out-Null
if (-not (dotnet sln $sln list | Select-String 'LibDat2')) {
    dotnet sln $sln add (Join-Path $Root 'external\VisualGGPK2\LibDat2\LibDat2.csproj')
}

Write-Host 'Building solution...'
dotnet build $sln -c Debug
Write-Host ''
Write-Host 'Done. Output: bin\Debug\'
Write-Host 'Run GUI:  bin\Debug\VisualGGPK3.exe'
Write-Host 'Run CLI:  bin\Debug\ExtractBundledGGPK3.exe'
