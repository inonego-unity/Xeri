param
(
    [switch]$Serve,
    [switch]$SkipMetadata,
    [switch]$VerifySnapshot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = $PSScriptRoot
$apiDir = Join-Path $repoRoot '.docfx/api'
$siteDir = Join-Path $repoRoot '_site'
$snapshotPath = Join-Path $repoRoot 'Docs/generated-api.zip'
$snapshotHashPath = Join-Path $repoRoot 'Docs/generated-api.sha256'

function Get-StringSha256([string]$value)
{
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try
    {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        return (($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally
    {
        $sha.Dispose()
    }
}
function Get-ApiSourceHash
{
    $roots = @(
        (Join-Path $repoRoot 'com.inonego.xeri/Runtime'),
        (Join-Path $repoRoot 'com.inonego.xeri/Editor')
    )

    $files = foreach ($root in $roots)
    {
        Get-ChildItem $root -Recurse -File |
            Where-Object { $_.Extension -in @('.cs', '.asmdef') }
    }

    $files += Get-Item (Join-Path $repoRoot 'com.inonego.xeri/package.json')
    $entries = foreach ($file in $files)
    {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
        Get-StringSha256 $normalized
    }

    return Get-StringSha256 (($entries | Sort-Object) -join "`n")
}

function Write-ApiSnapshot
{
    $stagingDir = Join-Path $repoRoot '.docfx/api-snapshot'
    $prefixBackslash = $repoRoot.TrimEnd('\', '/') + '\'
    $prefixSlash = $repoRoot.TrimEnd('\', '/') + '/'

    if (Test-Path $stagingDir)
    {
        Remove-Item $stagingDir -Recurse -Force
    }

    New-Item $stagingDir -ItemType Directory -Force | Out-Null

    try
    {
        foreach ($source in (Get-ChildItem $apiDir -File -Recurse))
        {
            $relative = $source.FullName.Substring($apiDir.Length).TrimStart('\', '/')
            $destination = Join-Path $stagingDir $relative
            $destinationDir = Split-Path $destination -Parent
            New-Item $destinationDir -ItemType Directory -Force | Out-Null

            if ($source.Extension -eq '.yml')
            {
                $text = [System.IO.File]::ReadAllText($source.FullName)
                $text = $text.Replace($prefixBackslash, '').Replace($prefixSlash, '')
                [System.IO.File]::WriteAllText(
                    $destination,
                    $text,
                    [System.Text.UTF8Encoding]::new($false)
                )
            }
            else
            {
                Copy-Item $source.FullName $destination -Force
            }
        }

        if (Test-Path $snapshotPath)
        {
            Remove-Item $snapshotPath -Force
        }

        if (-not ('System.IO.Compression.ZipFile' -as [type]))
        {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
        }

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $stagingDir,
            $snapshotPath,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
    }
    finally
    {
        if (Test-Path $stagingDir)
        {
            Remove-Item $stagingDir -Recurse -Force
        }
    }

    $sourceHash = Get-ApiSourceHash
    [System.IO.File]::WriteAllText(
        $snapshotHashPath,
        $sourceHash + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false)
    )
}

function Restore-ApiSnapshot
{
    if (-not (Test-Path $snapshotPath))
    {
        throw "API snapshot not found: $snapshotPath"
    }

    if (Test-Path $apiDir)
    {
        Remove-Item $apiDir -Recurse -Force
    }

    New-Item $apiDir -ItemType Directory -Force | Out-Null

    if (-not ('System.IO.Compression.ZipFile' -as [type]))
    {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
    }

    [System.IO.Compression.ZipFile]::ExtractToDirectory($snapshotPath, $apiDir)
}

function Assert-ApiSnapshotCurrent
{
    if (-not (Test-Path $snapshotHashPath))
    {
        throw "API snapshot hash not found: $snapshotHashPath"
    }

    $expected = ([System.IO.File]::ReadAllText($snapshotHashPath)).Trim()
    $actual = Get-ApiSourceHash
    if ($expected -ne $actual)
    {
        throw 'Generated API snapshot is stale. Run .\build-docs.ps1 on a Unity development machine and commit Docs/generated-api.*.'
    }
}
if ($VerifySnapshot)
{
    Assert-ApiSnapshotCurrent
    Write-Host 'API snapshot is current.'
    exit 0
}

Push-Location $repoRoot
try
{
    dotnet tool restore
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipMetadata)
    {
        if (Test-Path $apiDir)
        {
            Remove-Item $apiDir -Recurse -Force
        }

        Write-Host 'Generating DocFX API metadata...'
        dotnet docfx metadata docfx.json
        if ($LASTEXITCODE -ne 0)
        {
            throw "DocFX metadata failed with exit code $LASTEXITCODE."
        }

        Write-Host 'Writing sanitized API snapshot...'
        Write-ApiSnapshot
        Write-Host 'API snapshot updated.'
        Restore-ApiSnapshot
    }
    elseif (-not (Test-Path (Join-Path $apiDir 'toc.yml')))
    {
        Restore-ApiSnapshot
    }

    if (Test-Path $siteDir)
    {
        Remove-Item $siteDir -Recurse -Force
    }
    $buildArgs = @('docfx', 'build', 'docfx.json')
    if ($Serve)
    {
        $buildArgs += '--serve'
    }

    Write-Host 'Building DocFX site...'
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0)
    {
        throw "DocFX build failed with exit code $LASTEXITCODE."
    }
}
finally
{
    Pop-Location
}
