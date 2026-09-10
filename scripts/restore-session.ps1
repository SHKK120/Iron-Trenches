[CmdletBinding()]
param(
    [switch]$Online,
    [ValidateSet('ManagedPc', 'UnityIntegration')]
    [string]$Mode = 'ManagedPc'
)

$ErrorActionPreference = 'Stop'
$expectedRemote = 'https://github.com/SHKK120/Iron-Trenches.git'
$expectedUnityVersion = '6000.6.0f1'
$gitGuiPath = 'C:\Program Files\Git\cmd\git-gui.exe'
$repoRoot = Split-Path -Parent $PSScriptRoot
$expectedWorkspaceRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Iron-Trenches'
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

$resolvedRepoRoot = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\')
$resolvedExpectedRoot = [System.IO.Path]::GetFullPath($expectedWorkspaceRoot).TrimEnd('\')
if ($resolvedRepoRoot -eq $resolvedExpectedRoot) {
    Write-CheckResult 'PASS' 'Workspace root' $resolvedRepoRoot
}
else {
    Write-CheckResult 'FAIL' 'Workspace root' "expected $resolvedExpectedRoot, found $resolvedRepoRoot; do not work from a ZIP folder"
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
    'global.json',
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
    'docs/지시장부.md',
    'previews/README.md',
    'previews/index.html',
    'previews/RTS_CORE_03D_RouteThreatInterdiction.html',
    'previews/RTS_CORE_03C_ProductionReinforcement.html',
    'previews/RTS_CORE_03B_TerrainRoadReinforcement.html',
    'previews/RTS_PLAYTEST_03A_R1_TerrainRoadInterdiction.html',
    'previews/RTS_PLAYTEST_03A_SupplyStrategy.html',
    'previews/RTS_CORE_01C_Territory.html',
    'previews/RTS_CORE_02A_Economy.html',
    'previews/RTS_CORE_02B_FreeConstruction.html',
    'previews/RTS_CORE_02C_ConstructionOwnership.html',
    'previews/RTS_CORE_03A_SupplyConnectivity.html',
    'tools/ManagedPcChecks/ManagedPcChecks.csproj'
)

Write-Host 'Iron & Trenches Recovery Check'
Write-Host "Mode: $Mode"
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

$sourceDotnetPath = $null
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$standaloneDotnetPath = Join-Path $env:USERPROFILE '.dotnet-sdk-8.0.318\dotnet.exe'
$unityDotnetPath = "C:\Program Files\Unity\Hub\Editor\$expectedUnityVersion\Editor\Data\DotNetSdk\dotnet.exe"
if ($dotnetCommand -and (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $dotnetCommand.Source) 'sdk/8.0.318'))) {
    $sourceDotnetPath = $dotnetCommand.Source
    Write-CheckResult 'PASS' 'Source-only .NET SDK' "8.0.318 — $($dotnetCommand.Source)"
}
elseif ((Test-Path -LiteralPath $standaloneDotnetPath) -and (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $standaloneDotnetPath) 'sdk/8.0.318'))) {
    $sourceDotnetPath = $standaloneDotnetPath
    Write-CheckResult 'PASS' 'Source-only .NET SDK' "$standaloneDotnetPath — approved official standalone SDK"
}
elseif ((Test-Path -LiteralPath $unityDotnetPath) -and (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $unityDotnetPath) 'sdk/8.0.318'))) {
    $sourceDotnetPath = $unityDotnetPath
    Write-CheckResult 'PASS' 'Source-only .NET SDK' "$unityDotnetPath — verified managed-PC fallback"
}
else {
    Write-CheckResult 'BLOCKED' 'Source-only .NET SDK' 'not found; use an approved official .NET 8 SDK installation path and do not install Unity only for this check'
}

$managedChecksProject = Join-Path $repoRoot 'tools/ManagedPcChecks/ManagedPcChecks.csproj'
$managedChecksOutput = Join-Path $repoRoot 'Game/Temp/ManagedPcChecks'
if ($sourceDotnetPath) {
    $buildOutput = & $sourceDotnetPath build $managedChecksProject --configuration Release --output $managedChecksOutput --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-CheckResult 'FAIL' 'ManagedPcChecks build' (($buildOutput | Out-String).Trim())
    }
    else {
        Write-CheckResult 'PASS' 'ManagedPcChecks build' 'Release build completed with warnings treated as errors'
        $checksAssembly = Join-Path $managedChecksOutput 'ManagedPcChecks.dll'
        $checksOutput = & $sourceDotnetPath $checksAssembly 2>&1
        $checksText = (($checksOutput | Out-String).Trim())
        if ($LASTEXITCODE -eq 0 -and $checksText -match '^PASS ManagedPcChecks \(\d+ assertions\)$') {
            Write-CheckResult 'PASS' 'ManagedPcChecks run' $checksText
        }
        else {
            Write-CheckResult 'FAIL' 'ManagedPcChecks run' $checksText
        }
    }
}
else {
    Write-CheckResult 'SKIP' 'ManagedPcChecks' 'Source-only .NET SDK is unavailable'
}

$previewPath = Join-Path $repoRoot 'previews/index.html'
if (Test-Path -LiteralPath $previewPath) {
    Write-CheckResult 'PASS' 'Browser Playable Preview' $previewPath
}
else {
    Write-CheckResult 'FAIL' 'Browser Playable Preview Hub' 'previews/index.html missing'
}

$dashboardPath = Join-Path $repoRoot 'docs/40_구현순서.md'
if (Test-Path -LiteralPath $dashboardPath) {
    $dashboard = Get-Content -Raw -LiteralPath $dashboardPath
    if ($dashboard -match '## 정상 PC 병행 재개 Bundle' -and $dashboard -match 'DEV-BOOT-RESUME-01') {
        Write-CheckResult 'PASS' 'Next Result Bundle' 'DEV-BOOT-RESUME-01 found in docs/40_구현순서.md'
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
    if (Test-Path -LiteralPath $gitGuiPath) {
        Write-CheckResult 'PASS' 'Git GUI fallback' $gitGuiPath
    }
    else {
        Write-CheckResult 'BLOCKED' 'Git GUI fallback' 'approved Git GUI not found'
    }
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
            if (Test-Path -LiteralPath $gitGuiPath) {
                Write-CheckResult 'PASS' 'Git GUI fallback' $gitGuiPath
            }
            else {
                Write-CheckResult 'BLOCKED' 'Git GUI fallback' 'approved Git GUI not found'
            }
        }
    }
}

$unityHubPath = 'C:\Program Files\Unity Hub\Unity Hub.exe'
$unityHubVersionPath = 'C:\Program Files\Unity Hub\version'
if ($Mode -eq 'ManagedPc') {
    Write-CheckResult 'SKIP' 'Unity Hub' 'not required in ManagedPc mode; use Source-Only C# and Browser Preview'
}
elseif (Test-Path -LiteralPath $unityHubPath) {
    $hubVersion = if (Test-Path -LiteralPath $unityHubVersionPath) {
        (Get-Content -Raw -LiteralPath $unityHubVersionPath).Trim()
    }
    else {
        'version file missing'
    }
    Write-CheckResult 'PASS' 'Unity Hub' "$hubVersion — $unityHubPath"
}
else {
    Write-CheckResult 'FAIL' 'Unity Hub' 'not found at the verified install path'
}

$unityEditorPath = "C:\Program Files\Unity\Hub\Editor\$expectedUnityVersion\Editor\Unity.exe"
if ($Mode -eq 'ManagedPc') {
    Write-CheckResult 'SKIP' 'Unity Editor' 'not required in ManagedPc mode'
}
elseif (Test-Path -LiteralPath $unityEditorPath) {
    Write-CheckResult 'PASS' 'Unity Editor' "$expectedUnityVersion — $unityEditorPath"
}
else {
    Write-CheckResult 'FAIL' 'Unity Editor' "$expectedUnityVersion not found"
}

$windowsSupportPath = "C:\Program Files\Unity\Hub\Editor\$expectedUnityVersion\Editor\Data\PlaybackEngines\WindowsStandaloneSupport"
$webGlSupportPath = "C:\Program Files\Unity\Hub\Editor\$expectedUnityVersion\Editor\Data\PlaybackEngines\WebGLSupport"
foreach ($module in @(
    @{ Name = 'Windows Standalone Support'; Path = $windowsSupportPath },
    @{ Name = 'WebGL Build Support'; Path = $webGlSupportPath }
)) {
    if ($Mode -eq 'ManagedPc') {
        Write-CheckResult 'SKIP' $module.Name 'not required in ManagedPc mode'
    }
    elseif (Test-Path -LiteralPath $module.Path) {
        Write-CheckResult 'PASS' $module.Name $module.Path
    }
    else {
        Write-CheckResult 'FAIL' $module.Name 'required module not found'
    }
}

$visualStudioPath = 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe'
if ($Mode -eq 'ManagedPc') {
    Write-CheckResult 'SKIP' 'Visual Studio Community' 'not required in ManagedPc mode'
}
elseif (Test-Path -LiteralPath $visualStudioPath) {
    Write-CheckResult 'PASS' 'Visual Studio Community' $visualStudioPath

    $instanceRoot = 'C:\ProgramData\Microsoft\VisualStudio\Packages\_Instances'
    $stateFiles = if (Test-Path -LiteralPath $instanceRoot) {
        Get-ChildItem -LiteralPath $instanceRoot -Filter 'state.json' -File -Recurse -ErrorAction SilentlyContinue
    }
    else {
        @()
    }
    $stateText = ($stateFiles | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"

    if ($stateText -match 'Microsoft\.VisualStudio\.Workload\.ManagedGame') {
        Write-CheckResult 'PASS' 'Visual Studio Unity workload' 'Microsoft.VisualStudio.Workload.ManagedGame'
    }
    else {
        Write-CheckResult 'FAIL' 'Visual Studio Unity workload' 'registration not found'
    }

    if ($stateText -match 'Microsoft\.VisualStudio\.Component\.Unity') {
        Write-CheckResult 'PASS' 'Visual Studio Tools for Unity' 'Microsoft.VisualStudio.Component.Unity'
    }
    else {
        Write-CheckResult 'FAIL' 'Visual Studio Tools for Unity' 'registration not found'
    }
}
else {
    Write-CheckResult 'FAIL' 'Visual Studio Community' 'verified install path not found'
}

$runtimeFiles = @(
    'C:\Windows\System32\vcruntime140.dll',
    'C:\Windows\System32\msvcp140.dll',
    'C:\Windows\SysWOW64\vcruntime140.dll',
    'C:\Windows\SysWOW64\msvcp140.dll'
)
$missingRuntimeFiles = $runtimeFiles | Where-Object { -not (Test-Path -LiteralPath $_) }
if ($Mode -eq 'ManagedPc') {
    Write-CheckResult 'SKIP' 'Visual C++ runtime files' 'Unity toolchain prerequisite not required in ManagedPc mode'
}
elseif ($missingRuntimeFiles) {
    Write-CheckResult 'FAIL' 'Visual C++ runtime files' "missing: $($missingRuntimeFiles -join ', ')"
}
else {
    Write-CheckResult 'PASS' 'Visual C++ runtime files' 'x64 and x86 runtime DLLs found'
}

$unityProjectRoot = Join-Path $repoRoot 'Game'
$unityVersionPath = Join-Path $unityProjectRoot 'ProjectSettings/ProjectVersion.txt'
if (Test-Path -LiteralPath $unityVersionPath) {
    $unityVersionText = Get-Content -Raw -LiteralPath $unityVersionPath
    $unityVersion = if ($unityVersionText -match 'm_EditorVersion:\s*(.+)') { $Matches[1].Trim() } else { 'version marker unreadable' }
    if ($unityVersion -eq $expectedUnityVersion) {
        Write-CheckResult 'PASS' 'Unity project marker' $unityVersion
    }
    else {
        Write-CheckResult 'FAIL' 'Unity project marker' "expected $expectedUnityVersion, found $unityVersion"
    }
}
else {
    Write-CheckResult 'FAIL' 'Unity project' "missing $unityVersionPath"
}

$unityManifestPath = Join-Path $unityProjectRoot 'Packages/manifest.json'
if (Test-Path -LiteralPath $unityManifestPath) {
    try {
        $unityManifest = Get-Content -Raw -LiteralPath $unityManifestPath | ConvertFrom-Json
        $urpVersion = $unityManifest.dependencies.'com.unity.render-pipelines.universal'
        $inputVersion = $unityManifest.dependencies.'com.unity.inputsystem'

        if ($urpVersion -eq '17.6.0') {
            Write-CheckResult 'PASS' 'URP package' $urpVersion
        }
        else {
            Write-CheckResult 'FAIL' 'URP package' "expected 17.6.0, found $urpVersion"
        }

        if ($inputVersion -eq '1.20.0') {
            Write-CheckResult 'PASS' 'Input System package' $inputVersion
        }
        else {
            Write-CheckResult 'FAIL' 'Input System package' "expected 1.20.0, found $inputVersion"
        }
    }
    catch {
        Write-CheckResult 'FAIL' 'Unity package manifest' 'manifest.json could not be read'
    }
}
else {
    Write-CheckResult 'FAIL' 'Unity package manifest' "missing $unityManifestPath"
}

$mainDevScenePath = Join-Path $unityProjectRoot 'Assets/Scenes/SampleScene.unity'
if (Test-Path -LiteralPath $mainDevScenePath) {
    Write-CheckResult 'PASS' 'Main Dev Scene' $mainDevScenePath
}
else {
    Write-CheckResult 'FAIL' 'Main Dev Scene' "missing $mainDevScenePath"
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
