#Requires -Version 5.1
<#
.SYNOPSIS
  Keeps only the latest VisualGGPK3 semver release public; drafts all older ones.

.DESCRIPTION
  On public GitHub repos, published releases are always visible. Draft releases are
  visible only to collaborators — effectively "비공개" for visitors.

  Called automatically by Publish-GitHubRelease.ps1 after each release.
#>
param(
	[string]$Repo = 'Baegovda/GGPK_Custom',
	[string]$KeepTag = '',
	[switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SemVerFromTag([string]$Tag) {
	if ($Tag -match '^v(\d+\.\d+\.\d+)') {
		return [version]$Matches[1]
	}
	return $null
}

$json = gh release list --repo $Repo --limit 200 --json tagName,isDraft
if ($LASTEXITCODE -ne 0) {
	throw "gh release list failed (exit $LASTEXITCODE)"
}

$parsed = $json | ConvertFrom-Json
$releases = @(foreach ($item in $parsed) { $item })
if ($releases.Count -eq 0) {
	Write-Host 'No releases found.'
	return
}

$versioned = @(
	foreach ($release in $releases) {
		$ver = Get-SemVerFromTag ([string]$release.tagName)
		if ($null -eq $ver) { continue }
		[PSCustomObject]@{
			Tag = [string]$release.tagName
			Version = $ver
			Draft = [bool]$release.isDraft
		}
	}
)

if ($versioned.Count -eq 0) {
	Write-Host 'No semver releases (vX.Y.Z) found — nothing to adjust.'
	return
}

$keep = if (-not [string]::IsNullOrWhiteSpace($KeepTag)) {
	$KeepTag.Trim()
} else {
	$sorted = @($versioned | Sort-Object { $_.Version } -Descending)
	[string]$sorted[0].Tag
}

if ([string]::IsNullOrWhiteSpace($keep)) {
	throw 'Could not determine latest release tag.'
}

Write-Host "Latest public release: $keep"

foreach ($entry in ($versioned | Sort-Object Version -Descending)) {
	if ($entry.Tag -eq $keep) {
		if ($entry.Draft) {
			if ($WhatIf) {
				Write-Host "[WhatIf] Would publish latest: $($entry.Tag)"
			} else {
				gh release edit $entry.Tag --repo $Repo --draft=false | Out-Null
				Write-Host "Published (public): $($entry.Tag)"
			}
		}
		continue
	}

	if (-not $entry.Draft) {
		if ($WhatIf) {
			Write-Host "[WhatIf] Would draft (hide): $($entry.Tag)"
		} else {
			gh release edit $entry.Tag --repo $Repo --draft | Out-Null
			Write-Host "Drafted (hidden): $($entry.Tag)"
		}
	}
}
