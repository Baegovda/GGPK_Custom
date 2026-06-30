# Stop existing VisualGGPK3, build solution, launch app.
$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$ExeName = 'VisualGGPK3'

Get-Process -Name $ExeName -ErrorAction SilentlyContinue | ForEach-Object {
	Write-Host "Stopping $ExeName (PID $($_.Id))..."
	Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
if (Get-Process -Name $ExeName -ErrorAction SilentlyContinue) {
	Start-Sleep -Milliseconds 500
	Get-Process -Name $ExeName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

Write-Host 'Building LibGGPK3...'
Push-Location $Root
try {
	dotnet build (Join-Path $Root 'LibGGPK3.sln') -c Debug -v minimal
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

	$outDir = Join-Path $Root 'bin\Debug'
	$exe = Join-Path $outDir "$ExeName.exe"
	if (-not (Test-Path $exe)) {
		Write-Error "Not found: $exe"
		exit 1
	}

	Write-Host "Starting $ExeName..."
	Start-Process -FilePath $exe -WorkingDirectory $outDir
} finally {
	Pop-Location
}
