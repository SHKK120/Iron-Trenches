[CmdletBinding()]
param(
    [switch]$Online
)

$ErrorActionPreference = 'Stop'
$expectedRemote = 'https://github.com/SHKK120/Iron-Trenches.git'
$repoRoot = Split-Path -Parent $PSScriptRoot
$script:hasFailure = $false
$script:hasBlocked = $false

Set-Location -LiteralPath $repoRoot

function Write-CheckResult {
    param(
        [ValidateSet('PASS', 'FAIL', 'SKIP', 'BLOCKED')]
        [string]$State,
        [string]$Name,
        [string]$Detail
    )

    if ($State -eq 'FAIL') {
        $script:hasFailure = $true
    }
    elseif ($State -eq 'BLOCKED') {
        $script:hasBlocked = $true
    }

    Write-Host "[$State] $Name — $Detail"
}

function Invoke-GitInspection {
    param([string[]]$Arguments)

    $output = & $script:gitExecutable @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw (($output | Out-String).Trim())
    }

    return (($output | Out-String).Trim())
}

$requiredFiles = @(
    'AGENTS.md',
    'docs/00_시작.md',
    'docs/01_환경복구.md',
    'docs/10_정체성.md',
    'docs/11_규칙_전투.md',
    'docs/12_규칙_지휘.md',
    'docs/13_규칙_제압사기.md',
    'docs/14_규칙_보급.md',
    'docs/15_규칙_전장.md',
    'docs/20_UI_카메라.md',
    'docs/30_데이터구조.md',
    'docs/31_병종.md',
    'docs/40_구현순서.md',
    'docs/50_닫을것.md',
    'docs/60_나중에.md',
    'docs/지시장부.md'
)

Write-Host 'Iron & Trenches Recovery Check'
Write-Host ''

$missingFiles = $requiredFiles | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $repoRoot $_))
}

if ($missingFiles) {
    Write-CheckResult 'FAIL' 'Foundation files' "missing: $($missingFiles -join ', ')"
}
else {
    Write-CheckResult 'PASS' 'Foundation files' "$($requiredFiles.Count) required files found"
}

$dashboardPath = Join-Path $repoRoot 'docs/40_구현순서.md'
if (Test-Path -LiteralPath $dashboardPath) {
    $dashboard = Get-Content -Raw -LiteralPath $dashboardPath
    if ($dashboard -match '## 다음 Result Bundle' -and $dashboard -match 'DEV-BOOT-01') {
        Write-CheckResult 'PASS' 'Next Result Bundle' 'DEV-BOOT-01 found in docs/40_구현순서.md'
    }
    else {
        Write-CheckResult 'FAIL' 'Next Result Bundle' 'expected dashboard marker not found'
    }
}
else {
    Write-CheckResult 'FAIL' 'Next Result Bundle' 'dashboard file missing'
}

$gitDirectory = Join-Path $repoRoot '.git'
if (-not (Test-Path -LiteralPath $gitDirectory)) {
    Write-CheckResult 'FAIL' 'Git working copy' '.git not found; treat this folder as a ZIP/imported safety copy'
    Write-CheckResult 'SKIP' 'Repository identity' 'requires a normal Clone or approved remote workspace'
    Write-CheckResult 'SKIP' 'Remote checkpoint' 'requires a normal Clone or approved remote workspace'
}
else {
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if (-not $gitCommand) {
        Write-CheckResult 'BLOCKED' 'Git CLI' 'not available; use an already-approved Git GUI or the manual checklist'
    }
    else {
        $script:gitExecutable = $gitCommand.Source

        try {
            $null = Invoke-GitInspection @('--version')
            $insideWorkTree = Invoke-GitInspection @('rev-parse', '--is-inside-work-tree')
            if ($insideWorkTree -ne 'true') {
                Write-CheckResult 'FAIL' 'Git working copy' 'rev-parse did not confirm a worktree'
            }
            else {
                Write-CheckResult 'PASS' 'Git working copy' 'normal worktree confirmed'
            }

            $origin = Invoke-GitInspection @('remote', 'get-url', 'origin')
            if ($origin.TrimEnd('/') -eq $expectedRemote.TrimEnd('/')) {
                Write-CheckResult 'PASS' 'Repository identity' 'origin matches SHKK120/Iron-Trenches'
            }
            else {
                Write-CheckResult 'FAIL' 'Repository identity' "unexpected origin: $origin"
            }

            $branch = Invoke-GitInspection @('branch', '--show-current')
            if ($branch -eq 'main') {
                Write-CheckResult 'PASS' 'Branch' 'main'
            }
            else {
                Write-CheckResult 'FAIL' 'Branch' "current branch: $branch"
            }

            $workingChanges = Invoke-GitInspection @('status', '--porcelain')
            if ([string]::IsNullOrWhiteSpace($workingChanges)) {
                Write-CheckResult 'PASS' 'Local changes' 'working tree clean'
            }
            else {
                $changeCount = ($workingChanges -split "`r?`n").Count
                Write-CheckResult 'FAIL' 'Local changes' "$changeCount item(s) require human review"
            }

            if ($Online) {
                $localHead = Invoke-GitInspection @('rev-parse', 'HEAD')
                $remoteLine = Invoke-GitInspection @('ls-remote', 'origin', 'refs/heads/main')
                $remoteHead = ($remoteLine -split '\s+')[0]
                if ($localHead -eq $remoteHead) {
                    Write-CheckResult 'PASS' 'Remote checkpoint' "local HEAD matches origin/main: $localHead"
                }
                else {
                    Write-CheckResult 'FAIL' 'Remote checkpoint' 'local HEAD differs from origin/main; review manually'
                }
            }
            else {
                Write-CheckResult 'SKIP' 'Remote checkpoint' 'run with -Online when network inspection is allowed'
            }
        }
        catch {
            Write-CheckResult 'BLOCKED' 'Git CLI' 'execution blocked or unavailable; do not retry or bypass managed-PC policy'
        }
    }
}

$unityVersionPath = Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt'
if (Test-Path -LiteralPath $unityVersionPath) {
    $unityVersionText = Get-Content -Raw -LiteralPath $unityVersionPath
    $unityVersion = if ($unityVersionText -match 'm_EditorVersion:\s*(.+)') { $Matches[1].Trim() } else { 'version marker unreadable' }
    Write-CheckResult 'PASS' 'Unity project marker' $unityVersion
}
else {
    Write-CheckResult 'SKIP' 'Unity' 'DEV-BOOT-01 not completed'
}

if (Test-Path -LiteralPath (Join-Path $repoRoot '.agents/skills/wwi-recovery-smoke/SKILL.md')) {
    Write-CheckResult 'PASS' 'Recovery Skill' 'wwi-recovery-smoke found'
}
else {
    Write-CheckResult 'SKIP' 'Recovery Skill' 'created after Unity baseline is fixed'
}

Write-CheckResult 'SKIP' 'Unity MCP' 'not configured'

Write-Host ''
if ($script:hasFailure -or $script:hasBlocked) {
    Write-Host 'Recovery:'
    Write-Host 'PARTIAL'
}
else {
    Write-Host 'Recovery:'
    Write-Host 'READY'
}

Write-Host ''
Write-Host 'Next:'
Write-Host 'Read docs/40_구현순서.md'
