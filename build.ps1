<#
.SYNOPSIS
    Builds Param Patcher and packages the release zip.

.DESCRIPTION
    Builds "Param Patcher\src", then lays out the release in dist\ and zips it:

        dist\Dark-Souls-PTDE-Replayer-Convenience-Fixes.v<version>\
            Param Patcher\ParamPatcher.exe
            Param Patcher\mods\...        (from mods\; the exe reads mods from next to itself)
            Manual Install\...
            README.md                     (with every mod's list of changes written out)
        dist\Dark-Souls-PTDE-Replayer-Convenience-Fixes.v<version>.zip

    It also refreshes the change lists in the repo's README.md (see update-readme.ps1).

    Needs the .NET SDK (https://dotnet.microsoft.com/download). Players don't: the exe runs on
    .NET Framework 4.8, which comes with Windows 10 and 11.

.PARAMETER Version
    Version in the folder and zip name. Defaults to `git describe` (latest tag, or the commit), else today's date.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Version 1.2
#>
param([string]$Version)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $Version) {
    $Version = git -C $root describe --tags --always --dirty 2>$null
    if (-not $Version) { $Version = Get-Date -Format 'yyyy-MM-dd' }
}
# Release name: "Dark-Souls-PTDE-Replayer-Convenience-Fixes.v1.1" (a leading "v" in the version, e.g. from a git tag, isn't doubled).
$name = "Dark-Souls-PTDE-Replayer-Convenience-Fixes.v" + ($Version -replace '^[vV]', '')
$dist = Join-Path $root 'dist'
$out = Join-Path $dist $name
$buildDir = Join-Path $root 'Param Patcher\build'

Write-Host "Building Param Patcher..."
dotnet build (Join-Path $root 'Param Patcher\src\ParamPatcher.csproj') -c Release -o $buildDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

Write-Host "Laying out $name..."
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
$patcherOut = Join-Path $out 'Param Patcher'
New-Item -ItemType Directory $patcherOut | Out-Null
# The .config tells Windows the exe needs .NET Framework 4.8, so it can offer to install it if missing.
Copy-Item (Join-Path $buildDir 'ParamPatcher.exe'), (Join-Path $buildDir 'ParamPatcher.exe.config') $patcherOut
Copy-Item (Join-Path $root 'mods') (Join-Path $patcherOut 'mods') -Recurse
Copy-Item (Join-Path $root 'Manual Install') (Join-Path $out 'Manual Install') -Recurse
# Refresh the repo README's change lists, then give the release a README with them written out in full.
& (Join-Path $root 'update-readme.ps1')
& (Join-Path $root 'update-readme.ps1') -Plain -Output (Join-Path $out 'README.md')

$zip = "$out.zip"
if (Test-Path $zip) { Remove-Item $zip }
Add-Type -AssemblyName System.IO.Compression.FileSystem
# includeBaseDirectory: unzipping gives a single "Dark-Souls-PTDE-Replayer-Convenience-Fixes.v<version>" folder.
[IO.Compression.ZipFile]::CreateFromDirectory($out, $zip, [IO.Compression.CompressionLevel]::Optimal, $true)

$size = '{0:N1} MB' -f ((Get-Item $zip).Length / 1MB)
Write-Host "Done: $zip ($size)"
