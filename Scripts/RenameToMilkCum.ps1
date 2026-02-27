# 主项目重命名 EqualMilking -> MilkCum
# 运行位置：在 rjw-cummilk 仓库根目录执行 .\Scripts\RenameToMilkCum.ps1
$ErrorActionPreference = "Stop"
$root = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
if (-not (Test-Path "$root\Source\MilkCum")) {
    $root = (Get-Item $root).Parent.FullName
}
$src = Join-Path $root "Source\MilkCum"

# 所有 .cs 下 EqualMilking. -> MilkCum. （含 namespace / using / 全名）
Get-ChildItem -Path $src -Filter "*.cs" -Recurse | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.UTF8Encoding]::new($false))
    $orig = $content
    $content = $content -replace 'EqualMilking\.', 'MilkCum.'
    # Cumpilation Settings 里类型全名应为 MilkCum.Core.EqualMilkingSettings
    if ($_.FullName -match 'Cumpilation\\.*\\Settings\.cs$') {
        $content = $content -replace 'MilkCum\.EqualMilkingSettings', 'MilkCum.Core.EqualMilkingSettings'
    }
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Updated: $($_.FullName)"
    }
}

# Harmony ID（可选，保持兼容也可不改）
$modCs = Join-Path $src "Core\EqualMilkingMod.cs"
if (Test-Path $modCs) {
    $c = [System.IO.File]::ReadAllText($modCs, [System.Text.UTF8Encoding]::new($false))
    $c = $c -replace 'com\.akaster\.rimworld\.mod\.equalmilking', 'com.akaster.rimworld.mod.milkcum'
    [System.IO.File]::WriteAllText($modCs, $c, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Updated Harmony id: EqualMilkingMod.cs"
}

Write-Host "RenameToMilkCum.ps1 done."
