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
	[switch]$Force
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
	$tag = "v$version"

	$changelog = Get-Content 'CHANGELOG.md' -Raw
	$pattern = "(?ms)^## \[$([regex]::Escape($version))\][^\r\n]*\r?\n(.*?)(?=^## \[|\z)"
	if ($changelog -notmatch $pattern) {
		throw "No ## [$version] section in CHANGELOG.md — add it before publishing."
	}
	$notes = $Matches[1].Trim()
	if ([string]::IsNullOrWhiteSpace($notes)) {
		throw "CHANGELOG section for [$version] is empty."
	}

	$title = "VisualGGPK3 $version"
	$notesFile = Join-Path $env:TEMP "ggpk-release-$version.md"
	Set-Content -Path $notesFile -Value $notes -Encoding UTF8

	$tagExists = $false
	$null = git rev-parse $tag 2>$null
	if ($LASTEXITCODE -eq 0) { $tagExists = $true }

	if (-not $tagExists) {
		git tag -a $tag -m $title
		Write-Host "Created tag $tag"
	} elseif ($Force) {
		git tag -f -a $tag -m $title
		Write-Host "Recreated tag $tag"
	} else {
		Write-Host "Tag $tag already exists (use -Force to move it)."
	}

	$releaseExists = $false
	try {
		gh release view $tag 2>$null | Out-Null
		if ($LASTEXITCODE -eq 0) { $releaseExists = $true }
	} catch { }

	if ($releaseExists -and $Force) {
		gh release delete $tag --yes
		$releaseExists = $false
	}

	if (-not $releaseExists) {
		gh release create $tag --title $title --notes-file $notesFile
		Write-Host "Created GitHub Release $tag"
	} else {
		gh release edit $tag --title $title --notes-file $notesFile
		Write-Host "Updated GitHub Release $tag"
	}

	git push origin $tag
	if ($Force) { git push origin $tag --force }

	Write-Host "Done: https://github.com/Baegovda/GGPK_Custom/releases/tag/$tag"
} finally {
	Pop-Location
}
