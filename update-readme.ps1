<#
.SYNOPSIS
    Puts every mod's list of changes (mods\<Mod>\CHANGES.md) into the README.

.DESCRIPTION
    Replaces everything between the "<!-- changes:start -->" and "<!-- changes:end -->" lines of
    README.md with the change lists, so they're readable on the repo page. Edit the CHANGES.md
    files, not that part of the README: it's regenerated (build.ps1 runs this script).

    -Plain writes a version for the release zip instead: plain "### Mod" sections, no collapsible
    HTML blocks, since that README is often read in a text editor.

.PARAMETER Plain
    Plain sections instead of collapsible <details> blocks.

.PARAMETER Output
    Where to write the result. Defaults to README.md itself.

.EXAMPLE
    .\update-readme.ps1
    .\update-readme.ps1 -Plain -Output dist\Dark-Souls-PTDE-Replayer-Convenience-Fixes.v1.2\README.md
#>
param([switch]$Plain, [string]$Output)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$readme = Join-Path $root 'README.md'
if (-not $Output) { $Output = $readme }
$utf8 = New-Object System.Text.UTF8Encoding($false)

$text = [IO.File]::ReadAllText($readme, $utf8)
$nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$start = '<!-- changes:start -->'
$end = '<!-- changes:end -->'
$i = $text.IndexOf($start)
$j = $text.IndexOf($end)
if ($i -lt 0 -or $j -lt $i) { throw "README.md needs the lines '$start' and '$end' around the change lists" }

function ModName($dir) {
    $json = Join-Path $dir 'mod.json'
    if (Test-Path $json) {
        $name = (Get-Content $json -Raw -Encoding UTF8 | ConvertFrom-Json).name
        if ($name -is [string] -and $name) { return $name }
        if ($name -and $name.en) { return $name.en }
    }
    return Split-Path $dir -Leaf
}

$parts = @()
if (-not $Plain) {
    $parts += "Each mod's full list of changes. Click a mod to expand it; they can also be read in Param Patcher with ""Full List of Changes""."
}
foreach ($dir in Get-ChildItem (Join-Path $root 'mods') -Directory | Sort-Object Name) {
    $changes = Join-Path $dir.FullName 'CHANGES.md'
    if (-not (Test-Path $changes)) { continue }
    $name = ModName $dir.FullName
    # Headings inside a change list sit one level under the mod's own heading.
    $body = ([IO.File]::ReadAllText($changes, $utf8) -replace "`r`n", "`n").Trim()
    $body = [regex]::Replace($body, '(?m)^(#{1,4})(\s)', '$1##$2')
    if ($Plain) {
        $parts += "### $name`n`n$body"
    } else {
        $link = 'mods/' + [Uri]::EscapeDataString($dir.Name) + '/CHANGES.md'
        $parts += "<details>`n<summary><b>$name</b></summary>`n`n$body`n`n<sub>Source: <a href=""$link"">$link</a></sub>`n</details>"
    }
}
$section = ($parts -join "`n`n") -replace "`n", $nl

if ($Plain) {
    # No markers either: this copy is never regenerated.
    $result = $text.Substring(0, $i) + $section + $nl + $text.Substring($j + $end.Length)
} else {
    $result = $text.Substring(0, $i + $start.Length) + $nl + $nl + $section + $nl + $nl + $text.Substring($j)
}
$Output = [IO.Path]::GetFullPath($Output)
try { [IO.File]::WriteAllText($Output, $result, $utf8) }
catch [IO.IOException] {
    # The file is held open (e.g. by an editor's preview): swap in a finished copy instead.
    $tmp = "$Output.tmp"; $bak = "$Output.replace-bak"
    [IO.File]::WriteAllText($tmp, $result, $utf8)
    [IO.File]::Replace($tmp, $Output, $bak)
    # The old copy may still be held open by whatever had the README open; it's git-ignored.
    Remove-Item $bak -ErrorAction SilentlyContinue
}
Write-Host "Wrote the change lists of $(($parts | Where-Object { $_ -like '<details>*' -or $_ -like '### *' }).Count) mods into $Output"
