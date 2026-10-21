# Build a single-file exe.
# Usage: right-click "Run with PowerShell", or execute .\build.ps1 in a terminal.
# Keep this file ASCII-only: Windows PowerShell 5.1 misreads UTF-8 without BOM.
#
# If build\nuget exists (offline cache), it is used; otherwise NuGet.org is used.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root 'src\c2d\c2d.csproj'
$out  = Join-Path $root 'src\c2d\out'

$cache = Join-Path $root 'build\nuget\gpf'
$pkgs  = Join-Path $root 'build\nuget\pkgs'

Write-Host "Building..." -ForegroundColor Cyan

if ((Test-Path $cache) -and (Test-Path $pkgs)) {
    Write-Host "Using offline package cache." -ForegroundColor DarkGray
    $env:NUGET_PACKAGES = $cache
    dotnet publish $proj -c Release -r win-x64 -o $out --source $pkgs
} else {
    Write-Host "Using NuGet.org (first build may take a while)." -ForegroundColor DarkGray
    dotnet publish $proj -c Release -r win-x64 -o $out
}

$exe = Get-ChildItem $out -Filter *.exe -ErrorAction SilentlyContinue | Select-Object -First 1
if ($exe) {
    $mb = [math]::Round($exe.Length / 1MB, 2)
    Write-Host ""
    Write-Host "OK: $($exe.FullName) ($mb MB)" -ForegroundColor Green
} else {
    Write-Host "Build failed, see errors above." -ForegroundColor Red
}
