<#
.SYNOPSIS
  Build InventoryPeek and stage the Steam Workshop package.

.DESCRIPTION
  Produces .\package\ containing the About folder and the built DLL. With -Deploy, also
  copies it into the local mods folder so the in-game mod list picks it up.

.EXAMPLE
  .\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
    [string]$GameDir = $env:STATIONEERS_DIR,
    [switch]$Deploy,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $GameDir) {
    $GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Stationeers'
}
if (-not (Test-Path (Join-Path $GameDir 'rocketstation_Data\Managed\Assembly-CSharp.dll'))) {
    throw "Stationeers not found at '$GameDir'. Pass -GameDir or set STATIONEERS_DIR."
}

# The version is written in three places and nothing else keeps them in step. A published
# build and a local build sharing a version number cannot be told apart afterwards.
$csproj = Join-Path $root 'src\InventoryPeek.csproj'
$plugin = Join-Path $root 'src\InventoryPeekPlugin.cs'
$about = Join-Path $root 'About\About.xml'

$versions = [ordered]@{
    'csproj'   = ([regex]::Match((Get-Content $csproj -Raw), '<Version>([^<]+)</Version>')).Groups[1].Value
    'plugin'   = ([regex]::Match((Get-Content $plugin -Raw), 'pluginVersion\s*=\s*"([^"]+)"')).Groups[1].Value
    'About.xml' = ([regex]::Match((Get-Content $about -Raw), '<Version>([^<]+)</Version>')).Groups[1].Value
}
# @() so a single shared version stays an array; indexing a bare string yields one char.
$distinct = @($versions.Values | Sort-Object -Unique)
if ($distinct.Count -ne 1 -or [string]::IsNullOrWhiteSpace($distinct[0])) {
    $detail = ($versions.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', '
    throw "Version mismatch: $detail. Bump all three to the same value."
}
$version = $distinct[0]

Write-Host "Building $version against $GameDir"
dotnet build $csproj `
    -c $Configuration -p:GameDir="$GameDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "src\bin\$Configuration\InventoryPeek.dll"
if (-not (Test-Path $dll)) { throw "Build produced no DLL at $dll" }

$package = Join-Path $root 'package'
if (Test-Path $package) { Remove-Item $package -Recurse -Force }
New-Item -ItemType Directory -Path $package | Out-Null
Copy-Item (Join-Path $root 'About') $package -Recurse
Copy-Item $dll $package

# Steam rejects workshop previews over 1 MB.
$thumbPath = Join-Path $package 'About\thumb.png'
if ((Test-Path $thumbPath) -and (Get-Item $thumbPath).Length -gt 1MB) {
    Write-Warning "About\thumb.png is over 1 MB. Steam caps previews at 1 MB and the game will silently fall back to a blank image."
}

Write-Host "Staged $package"

if ($Deploy) {
    $mods = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Stationeers\mods\InventoryPeek'
    if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
    New-Item -ItemType Directory -Path $mods -Force | Out-Null
    Copy-Item "$package\*" $mods -Recurse
    Write-Host "Deployed to $mods"
    Write-Host 'Launch Stationeers; the mod appears in the in-game mods list as a local mod.'
}
