# 方案 A 重构：路径与命名空间一致
# 运行位置：在 rjw-cummilk 仓库根目录执行 .\Scripts\RefactorSourceStructure.ps1
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
if (-not (Test-Path "$root\Source\MilkCum\MilkCum.csproj")) {
    $root = (Get-Item $root).Parent.FullName
    if (-not (Test-Path "$root\Source\MilkCum\MilkCum.csproj")) { throw "Run from repo root or Scripts folder." }
}
$src = Join-Path $root "Source\MilkCum"

function Transform-Content {
    param([string]$content, [string]$newNamespace)
    $c = $content
    $c = $c -replace 'using EqualMilking\.HarmonyPatches;', 'using EqualMilking.Milk.HarmonyPatches;'
    $c = $c -replace 'using EqualMilking\.Helpers;', 'using EqualMilking.Milk.Helpers;'
    $c = $c -replace 'using EqualMilking\.Comps;', 'using EqualMilking.Milk.Comps;'
    $c = $c -replace 'using EqualMilking\.Data;', 'using EqualMilking.Milk.Data;'
    $c = $c -replace 'using EqualMilking\.Givers;', 'using EqualMilking.Milk.Givers;'
    $c = $c -replace 'using EqualMilking\.Jobs;', 'using EqualMilking.Milk.Jobs;'
    $c = $c -replace 'using EqualMilking\.Thoughts;', 'using EqualMilking.Milk.Thoughts;'
    $c = $c -replace 'using EqualMilking\.World;', 'using EqualMilking.Milk.World;'
    $c = $c -replace 'using EqualMilking;', 'using EqualMilking.Core;'
    $c = $c -replace 'global::EqualMilking\.EMDefOf', 'global::EqualMilking.Core.EMDefOf'
    $c = $c -replace 'namespace EqualMilking\.HarmonyPatches;', 'namespace EqualMilking.Milk.HarmonyPatches;'
    $c = $c -replace 'namespace EqualMilking\.Helpers;', 'namespace EqualMilking.Milk.Helpers;'
    $c = $c -replace 'namespace EqualMilking\.Comps;', 'namespace EqualMilking.Milk.Comps;'
    $c = $c -replace 'namespace EqualMilking\.Data;', 'namespace EqualMilking.Milk.Data;'
    $c = $c -replace 'namespace EqualMilking\.Givers;', 'namespace EqualMilking.Milk.Givers;'
    $c = $c -replace 'namespace EqualMilking;', "namespace $newNamespace;"
    return $c
}

$dirs = @("Core", "Milk\Comps", "Milk\Jobs", "Milk\Givers", "Milk\Helpers", "Milk\HarmonyPatches", "Milk\Thoughts", "Milk\World", "Milk\Data")
foreach ($d in $dirs) {
    $path = Join-Path $src $d
    if (-not (Test-Path $path)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }
}

$coreFiles = @("EqualMilkingMod.cs", "EqualMilking.cs", "EqualMilkingSettings.cs", "EMDefOf.cs")
foreach ($f in $coreFiles) {
    $from = Join-Path $src $f
    if (-not (Test-Path $from)) { continue }
    $content = Get-Content -Path $from -Raw -Encoding UTF8
    $content = Transform-Content -content $content -newNamespace "EqualMilking.Core"
    $to = Join-Path $src "Core\$f"
    [System.IO.File]::WriteAllText($to, $content, [System.Text.UTF8Encoding]::new($false))
}

foreach ($dirName in @("Data","Comps","Jobs","Givers","Helpers","HarmonyPatches")) {
    $fromDir = Join-Path $src $dirName
    if (-not (Test-Path $fromDir)) { continue }
    $ns = switch ($dirName) {
        "Data" { "EqualMilking.Milk.Data" }
        "Comps" { "EqualMilking.Milk.Comps" }
        "Jobs" { "EqualMilking.Milk.Jobs" }
        "Givers" { "EqualMilking.Milk.Givers" }
        "Helpers" { "EqualMilking.Milk.Helpers" }
        "HarmonyPatches" { "EqualMilking.Milk.HarmonyPatches" }
    }
    Get-ChildItem -Path $fromDir -Filter "*.cs" | ForEach-Object {
        $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
        $content = Transform-Content -content $content -newNamespace $ns
        $to = Join-Path $src "Milk\$dirName\$($_.Name)"
        [System.IO.File]::WriteAllText($to, $content, [System.Text.UTF8Encoding]::new($false))
    }
}

Get-ChildItem -Path $src -Filter "ThoughtWorker_*.cs" -File -ErrorAction SilentlyContinue | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
    $content = Transform-Content -content $content -newNamespace "EqualMilking.Milk.Thoughts"
    $to = Join-Path $src "Milk\Thoughts\$($_.Name)"
    [System.IO.File]::WriteAllText($to, $content, [System.Text.UTF8Encoding]::new($false))
}
Get-ChildItem -Path $src -Filter "WorldComponent_*.cs" -File -ErrorAction SilentlyContinue | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
    $content = Transform-Content -content $content -newNamespace "EqualMilking.Milk.World"
    $to = Join-Path $src "Milk\World\$($_.Name)"
    [System.IO.File]::WriteAllText($to, $content, [System.Text.UTF8Encoding]::new($false))
}

$uiDir = Join-Path $src "UI"
if (Test-Path $uiDir) {
    Get-ChildItem -Path $uiDir -Filter "*.cs" -Recurse | ForEach-Object {
        $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
        $content = Transform-Content -content $content -newNamespace "EqualMilking.UI"
        [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($false))
    }
}

$cumpDir = Join-Path $src "Cumpilation"
if (Test-Path $cumpDir) {
    Get-ChildItem -Path $cumpDir -Filter "*.cs" -Recurse | ForEach-Object {
        $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
        $content = $content -replace 'using EqualMilking\.HarmonyPatches;', 'using EqualMilking.Milk.HarmonyPatches;'
        $content = $content -replace 'using EqualMilking\.Helpers;', 'using EqualMilking.Milk.Helpers;'
        $content = $content -replace 'using EqualMilking\.Comps;', 'using EqualMilking.Milk.Comps;'
        $content = $content -replace 'using EqualMilking\.Data;', 'using EqualMilking.Milk.Data;'
        $content = $content -replace 'using EqualMilking;', 'using EqualMilking.Core;'
        $content = $content -replace 'global::EqualMilking\.EMDefOf', 'global::EqualMilking.Core.EMDefOf'
        [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($false))
    }
}

foreach ($f in $coreFiles) {
    $old = Join-Path $src $f
    if (Test-Path $old) { Remove-Item $old -Force }
}
Get-ChildItem -Path $src -Filter "ThoughtWorker_*.cs" -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $src -Filter "WorldComponent_*.cs" -File -ErrorAction SilentlyContinue | Remove-Item -Force
@("Data", "Comps", "Jobs", "Givers", "Helpers", "HarmonyPatches") | ForEach-Object {
    $dir = Join-Path $src $_
    if (Test-Path $dir) { Remove-Item -Path $dir -Recurse -Force }
}
Write-Host "Refactor done. Build and fix any remaining references."
