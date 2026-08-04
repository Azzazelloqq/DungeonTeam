[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [switch]$AllAssets
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Add-Failure
{
    param([string]$Message)
    $failures.Add($Message)
}

function Add-Warning
{
    param([string]$Message)
    $warnings.Add($Message)
}

function Get-FullProjectPath
{
    param([string]$RelativePath)
    return Join-Path $ProjectRoot ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
}

function Get-ChangedPaths
{
    $paths = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $tracked = & git -C $ProjectRoot -c core.quotePath=false -c core.safecrlf=false diff --name-only --diff-filter=ACMR HEAD -- 2>&1
    $trackedExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    if ($trackedExitCode -ne 0)
    {
        throw "Unable to read tracked changes: $($tracked -join [Environment]::NewLine)"
    }

    $ErrorActionPreference = 'Continue'
    $untracked = & git -C $ProjectRoot -c core.quotePath=false ls-files --others --exclude-standard 2>&1
    $untrackedExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    if ($untrackedExitCode -ne 0)
    {
        throw "Unable to read untracked files: $($untracked -join [Environment]::NewLine)"
    }

    foreach ($path in @($tracked | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }) +
                          @($untracked | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }))
    {
        if (-not [string]::IsNullOrWhiteSpace($path))
        {
            [void]$paths.Add($path.Trim().Replace('\', '/'))
        }
    }

    return @($paths)
}

function Test-UnityMetaCoverage
{
    param([string[]]$AssetPaths)

    $checkedDirectories = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($relativePath in $AssetPaths)
    {
        $fullPath = Get-FullProjectPath $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf))
        {
            continue
        }

        if ($relativePath.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase))
        {
            $guidMatch = Select-String -LiteralPath $fullPath -Pattern '^guid:\s*([0-9a-fA-F]{32})\s*$' | Select-Object -First 1
            if ($null -eq $guidMatch)
            {
                Add-Failure "Meta file has no valid GUID: $relativePath"
            }
        }
        elseif (-not (Test-Path -LiteralPath "$fullPath.meta" -PathType Leaf))
        {
            Add-Failure "Missing meta file: $relativePath.meta"
        }

        $directory = [IO.Path]::GetDirectoryName($relativePath).Replace('\', '/')
        while (-not [string]::IsNullOrWhiteSpace($directory) -and
               -not $directory.Equals('Assets', [StringComparison]::OrdinalIgnoreCase))
        {
            if ($checkedDirectories.Add($directory))
            {
                $directoryMeta = Get-FullProjectPath "$directory.meta"
                if (-not (Test-Path -LiteralPath $directoryMeta -PathType Leaf))
                {
                    Add-Failure "Missing folder meta file: $directory.meta"
                }
            }

            $directory = [IO.Path]::GetDirectoryName($directory)
            if ($null -ne $directory)
            {
                $directory = $directory.Replace('\', '/')
            }
        }
    }
}

function Get-MetaGuidRecords
{
    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($relativeRoot in @('Assets', 'Packages', 'Library/PackageCache'))
    {
        $fullRoot = Join-Path $ProjectRoot $relativeRoot
        if (Test-Path -LiteralPath $fullRoot -PathType Container)
        {
            $roots.Add($fullRoot)
        }
    }

    $ripgrep = Get-Command rg -ErrorAction SilentlyContinue
    if ($null -ne $ripgrep)
    {
        $previousErrorPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $output = & $ripgrep.Source --no-heading --with-filename --line-number --glob '*.meta' '^guid:\s*[0-9a-fA-F]{32}\s*$' @roots 2>&1
        $rgExitCode = $LASTEXITCODE
        $ErrorActionPreference = $previousErrorPreference
        if ($rgExitCode -notin @(0, 1))
        {
            throw "Unable to scan Unity GUIDs: $($output -join [Environment]::NewLine)"
        }

        foreach ($line in $output)
        {
            if ($line -match '^(.*?):\d+:guid:\s*([0-9a-fA-F]{32})\s*$')
            {
                [PSCustomObject]@{
                    Path = [IO.Path]::GetFullPath($Matches[1])
                    Guid = $Matches[2].ToLowerInvariant()
                }
            }
        }

        return
    }

    Add-Warning 'rg is unavailable; GUID scanning uses the slower PowerShell fallback.'
    foreach ($root in $roots)
    {
        foreach ($metaFile in Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.meta' -ErrorAction SilentlyContinue)
        {
            $match = Select-String -LiteralPath $metaFile.FullName -Pattern '^guid:\s*([0-9a-fA-F]{32})\s*$' | Select-Object -First 1
            if ($null -ne $match)
            {
                [PSCustomObject]@{
                    Path = $metaFile.FullName
                    Guid = $match.Matches[0].Groups[1].Value.ToLowerInvariant()
                }
            }
        }
    }
}

function Test-Guids
{
    param(
        [object[]]$GuidRecords,
        [string[]]$AssetPaths
    )

    $knownGuids = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $reportedUnresolved = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $assetGuidOwners = @{}
    $assetRoot = [IO.Path]::GetFullPath((Join-Path $ProjectRoot 'Assets'))
    foreach ($record in $GuidRecords)
    {
        [void]$knownGuids.Add($record.Guid)
        if (-not $record.Path.StartsWith($assetRoot, [StringComparison]::OrdinalIgnoreCase))
        {
            continue
        }

        if ($assetGuidOwners.ContainsKey($record.Guid))
        {
            Add-Failure "Duplicate asset GUID $($record.Guid): $($assetGuidOwners[$record.Guid]) and $($record.Path)"
        }
        else
        {
            $assetGuidOwners[$record.Guid] = $record.Path
        }
    }

    foreach ($relativePath in $AssetPaths)
    {
        $extension = [IO.Path]::GetExtension($relativePath)
        if ($extension -notin @('.prefab', '.unity'))
        {
            continue
        }

        $fullPath = Get-FullProjectPath $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf))
        {
            continue
        }

        $matches = Select-String -LiteralPath $fullPath -Pattern 'guid:\s*([0-9a-fA-F]{32})' -AllMatches
        foreach ($lineMatch in $matches)
        {
            foreach ($match in $lineMatch.Matches)
            {
                $guid = $match.Groups[1].Value.ToLowerInvariant()
                if ($guid -match '^0{32}$' -or
                    $guid -match '^0{16}[ef]0{15}$' -or
                    $knownGuids.Contains($guid))
                {
                    continue
                }

                $unresolvedKey = "$relativePath|$guid"
                if ($reportedUnresolved.Add($unresolvedKey))
                {
                    Add-Failure "Unresolved GUID $guid in $relativePath at line $($lineMatch.LineNumber)"
                }
            }
        }
    }
}

function Test-Asmdefs
{
    param([string[]]$ChangedPaths)

    $assemblies = @{}
    foreach ($asmdefFile in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File -Filter '*.asmdef')
    {
        try
        {
            $definition = Get-Content -Raw -LiteralPath $asmdefFile.FullName | ConvertFrom-Json
        }
        catch
        {
            Add-Failure "Invalid asmdef JSON: $($asmdefFile.FullName): $($_.Exception.Message)"
            continue
        }

        if ([string]::IsNullOrWhiteSpace($definition.name))
        {
            Add-Failure "Asmdef has no name: $($asmdefFile.FullName)"
            continue
        }

        if ($assemblies.ContainsKey($definition.name))
        {
            Add-Failure "Duplicate assembly name $($definition.name): $($assemblies[$definition.name].Path) and $($asmdefFile.FullName)"
            continue
        }

        $assemblies[$definition.name] = [PSCustomObject]@{
            Path = $asmdefFile.FullName
            Definition = $definition
        }
    }

    foreach ($relativePath in $ChangedPaths | Where-Object { $_.EndsWith('.asmdef', [StringComparison]::OrdinalIgnoreCase) })
    {
        $fullPath = Get-FullProjectPath $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf))
        {
            continue
        }

        try
        {
            $definition = Get-Content -Raw -LiteralPath $fullPath | ConvertFrom-Json
        }
        catch
        {
            continue
        }

        $name = [string]$definition.name
        $isTestAssembly = $name -match '\.Tests(?:\.|$)'
        $isEditorAssembly = $name -match '\.Editor(?:\.|$)'
        if (($name -match '\.(Domain|Application)$') -and $definition.noEngineReferences -ne $true)
        {
            Add-Failure "$name must set noEngineReferences to true."
        }

        foreach ($referenceValue in @($definition.references))
        {
            $reference = [string]$referenceValue
            if ([string]::IsNullOrWhiteSpace($reference) -or
                $reference.StartsWith('GUID:', [StringComparison]::OrdinalIgnoreCase) -or
                -not $assemblies.ContainsKey($reference))
            {
                continue
            }

            if (-not $isTestAssembly -and $reference -match '\.Tests(?:\.|$)')
            {
                Add-Failure "$name must not reference test assembly $reference."
            }

            if (-not $isTestAssembly -and -not $isEditorAssembly -and $reference -match '\.Editor(?:\.|$)')
            {
                Add-Failure "$name must not reference editor assembly $reference."
            }

            if ($name -match '\.Domain$' -and $reference -match '\.(Application|Runtime|Infrastructure)(?:\.|$)')
            {
                Add-Failure "$name has an invalid outward dependency on $reference."
            }
            elseif ($name -match '\.Application$' -and $reference -match '\.(Runtime|Infrastructure)(?:\.|$)')
            {
                Add-Failure "$name has an invalid outward dependency on $reference."
            }
            elseif ($name -match '\.Runtime$' -and $reference -match '\.Infrastructure(?:\.|$)')
            {
                Add-Failure "$name must not reference infrastructure assembly $reference."
            }
            elseif ($name -match '\.Infrastructure$' -and $reference -match '\.Runtime(?:\.|$)')
            {
                Add-Failure "$name must not reference runtime assembly $reference."
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot))
{
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
}
else
{
    $ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
}

$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$gitRoot = & git -C $ProjectRoot rev-parse --show-toplevel 2>&1
$gitRootExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
if ($gitRootExitCode -ne 0)
{
    throw "Project root is not a Git working tree: $ProjectRoot"
}

$ProjectRoot = [IO.Path]::GetFullPath([string]$gitRoot)
if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot 'Assets') -PathType Container))
{
    throw "Unity Assets directory was not found under $ProjectRoot"
}

if ($AllAssets)
{
    $assetPaths = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File |
        ForEach-Object { $_.FullName.Substring($ProjectRoot.Length + 1).Replace('\', '/') }
    $changedPaths = @($assetPaths)
}
else
{
    $changedPaths = @(Get-ChangedPaths)
    $assetPaths = @($changedPaths | Where-Object {
        $_.StartsWith('Assets/', [StringComparison]::OrdinalIgnoreCase)
    })
}

Test-UnityMetaCoverage $assetPaths
$guidRecords = @(Get-MetaGuidRecords)
Test-Guids $guidRecords $assetPaths
Test-Asmdefs $changedPaths

$ErrorActionPreference = 'Continue'
$diffCheck = & git -C $ProjectRoot -c core.safecrlf=false diff --check HEAD -- 2>&1
$diffCheckExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
if ($diffCheckExitCode -ne 0)
{
    foreach ($line in $diffCheck)
    {
        Add-Failure "git diff --check: $line"
    }
}

foreach ($warning in $warnings)
{
    Write-Warning $warning
}

if ($failures.Count -gt 0)
{
    Write-Host "Unity change validation failed with $($failures.Count) issue(s):" -ForegroundColor Red
    foreach ($failure in $failures)
    {
        Write-Host "  - $failure" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Unity change validation passed." -ForegroundColor Green
Write-Host "Checked $($assetPaths.Count) Unity asset file(s), $($guidRecords.Count) GUID record(s), and changed asmdef boundaries."
exit 0
