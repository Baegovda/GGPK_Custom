#Requires -Version 5.1
<#
.SYNOPSIS
  Builds a portable VisualGGPK3 zip for GitHub Release upload.

.DESCRIPTION
  Publishes VisualGGPK3 for win-x64 (default) or win-arm64, then creates
  VisualGGPK3-win-<arch>.zip in scripts/release-packages/.

  Run from repo root. Requires .NET 8 SDK.
#>
param(
	[string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
	[ValidateSet('win-x64', 'win-arm64')]
	[string]$Runtime = 'win-x64',
	[string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Push-Location $RepoRoot

try {
	$props = Get-Content 'Directory.Build.props' -Raw
	if ($props -notmatch '<Version>([^<]+)</Version>') {
		throw 'Could not parse <Version> from Directory.Build.props'
	}
	$version = $Matches[1].Trim()
	$arch = ($Runtime -split '-', 2)[1]
	$assetName = "VisualGGPK3-$Runtime.zip"

	$publishDir = Join-Path $RepoRoot "publish/$Runtime"
	$packageDir = Join-Path $PSScriptRoot 'release-packages'
	$zipPath = Join-Path $packageDir $assetName

	if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
	New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

	Write-Host "Publishing VisualGGPK3 $version ($Runtime)..."
	dotnet publish 'Examples/VisualGGPK3/VisualGGPK3.csproj' `
		-c $Configuration `
		-r $Runtime `
		-o $publishDir `
		--no-self-contained `
		-p:PublishReadyToRun=true `
		-nologo

	if (-not (Test-Path (Join-Path $publishDir 'VisualGGPK3.exe'))) {
		throw "VisualGGPK3.exe was not produced in $publishDir"
	}

	if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
	Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

	Write-Host "Created $zipPath"
	Write-Output $zipPath
} finally {
	Pop-Location
}
