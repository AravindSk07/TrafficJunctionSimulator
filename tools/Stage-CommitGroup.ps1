param(
    [Parameter(Mandatory=$true)][ValidateRange(1,20)][int]$Group,
    [switch]$Stage
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$plan = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'commit-groups.json') -Raw | ConvertFrom-Json
$entry = $plan | Where-Object { $_.id -eq $Group }
$files = @($entry.paths | ForEach-Object {
    Get-ChildItem -Path (Join-Path $projectRoot $_) -File | ForEach-Object {
        $_.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
    }
} | Sort-Object -Unique)
if ($files.Count -eq 0) { throw 'This group matched no files.' }
Write-Output "Group $Group - $($entry.message)"
$files | ForEach-Object { Write-Output "  $_" }
if (-not $Stage) {
    Write-Output 'Preview only. Add -Stage to stage these files. This script never commits.'
    exit 0
}
Push-Location $projectRoot
try {
    $root = git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $root) { throw 'Create a local repository in this project folder first.' }
    if ((Resolve-Path $root).Path -ne $projectRoot) { throw 'Refusing to stage into a parent repository. Initialize this project as its own repository.' }
    $staged = @(git diff --cached --name-only)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the Git index.' }
    if ($staged.Count -gt 0) { throw 'The index already contains staged work. Commit or unstage it before selecting another group.' }
    & git add -- $files
    if ($LASTEXITCODE -ne 0) { throw 'Git staging failed.' }
    Write-Output 'Files staged. Inspect the staged diff in SourceTree, then commit with the message above.'
} finally { Pop-Location }
