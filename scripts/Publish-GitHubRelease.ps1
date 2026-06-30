#Requires -Version 5.1
<#
.SYNOPSIS
  Tags the current commit and creates/updates a GitHub Release from CHANGELOG.md.

.DESCRIPTION
  Reads <Version> from Directory.Build.props, extracts ## [version] section from CHANGELOG.md,
  runs: git tag vX.Y.Z (if missing), gh release create, git push origin vX.Y.Z

  Run from repo root after commit. Requires: git, gh (authenticated).
#>
param(
	[string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
	[string]$Version = '',
	[switch]$Force,
	[switch]$Package
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Push-Location $RepoRoot

try {
	$props = Get-Content 'Directory.Build.props' -Raw
	if ([string]::IsNullOrWhiteSpace($Version)) {
		if ($props -notmatch '<Version>([^<]+)</Version>') {
			throw 'Could not parse <Version> from Directory.Build.props'
		}
		$Version = $Matches[1].Trim()
	}
	$tag = "v$Version"

	$changelog = Get-Content 'CHANGELOG.md' -Raw
	$pattern = "(?ms)^## \[$([regex]::Escape($Version))\][^\r\n]*\r?\n(.*?)(?=^## \[|\z)"
	if ($changelog -notmatch $pattern) {
		throw "No ## [$Version] section in CHANGELOG.md — add it before publishing."
	}
	$notes = $Matches[1].Trim()
	if ([string]::IsNullOrWhiteSpace($notes)) {
		throw "CHANGELOG section for [$Version] is empty."
	}

	$title = "VisualGGPK3 $Version"
	$notesFile = Join-Path $env:TEMP "ggpk-release-$Version.md"
	Set-Content -Path $notesFile -Value $notes -Encoding UTF8

	$tagExists = $false
	$prevEap = $ErrorActionPreference
	$ErrorActionPreference = 'SilentlyContinue'
	git rev-parse --verify "refs/tags/$tag" *> $null
	if ($LASTEXITCODE -eq 0) { $tagExists = $true }
	$ErrorActionPreference = $prevEap

	if (-not $tagExists) {
		git tag -a $tag -m $title
		Write-Host "Created tag $tag on HEAD"
	} elseif ($Force) {
		git tag -f -a $tag -m $title
		Write-Host "Recreated tag $tag"
	} else {
		Write-Host "Tag $tag already exists (use -Force to move it)."
	}

	git push origin $tag
	if ($Force) { git push origin $tag --force }

	$repo = 'Baegovda/GGPK_Custom'
	$releaseExists = $false
	$ErrorActionPreference = 'SilentlyContinue'
	gh release view $tag --repo $repo *> $null
	if ($LASTEXITCODE -eq 0) { $releaseExists = $true }
	$ErrorActionPreference = $prevEap

	if ($releaseExists -and $Force) {
		gh release delete $tag --repo $repo --yes
		$releaseExists = $false
	}

	if (-not $releaseExists) {
		gh release create $tag --repo $repo --title $title --notes-file $notesFile
		Write-Host "Created GitHub Release $tag"
	} else {
		gh release edit $tag --repo $repo --title $title --notes-file $notesFile
		Write-Host "Updated GitHub Release $tag"
	}

	if ($Package) {
		$packageScript = Join-Path $PSScriptRoot 'Package-VisualGGPK3Release.ps1'
		if (-not (Test-Path -LiteralPath $packageScript)) {
			throw "Missing $packageScript"
		}
		$zipPath = & $packageScript -RepoRoot $RepoRoot
		if (-not (Test-Path -LiteralPath $zipPath)) {
			throw "Package script did not produce a zip: $zipPath"
		}
		gh release upload $tag $zipPath --repo $repo --clobber
		Write-Host "Uploaded $(Split-Path -Leaf $zipPath)"
	}

	Write-Host "Done: https://github.com/$repo/releases/tag/$tag"
} finally {
	Pop-Location
}
