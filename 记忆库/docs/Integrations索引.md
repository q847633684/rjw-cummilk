# Integrations 索引

本 mod 与其它 mod/系统的集成入口一览，便于查找与开关。RJW、PipeSystem、VME_Patch 已收口到 `Source/Integrations/` 目录，命名空间未改。

---

## 一、集成模块列表

| 集成对象 | 路径 | 命名空间 | Harmony 实例 ID | 说明 |
|----------|------|----------|-----------------|------|
| **RJW** | `Source/Integrations/RJW/` | `MilkCum.RJW` | `com.akaster.rimworld.mod.equalmilking.rjw` | 奶量/奶 Def Patch、RJW 分娩、性行为后泌乳、生育力、乳房尺寸、Lust 吸奶等；独立 Harmony 仅 Patch 指定类。 |
| **PipeSystem (VEF)** | `Source/Integrations/PipeSystem/` | `MilkCum.PipeSystem` | `com.akaster.rimworld.mod.equalmilking.pipe` | 奶管道、龙头、容器；独立 Harmony.PatchAll()。 |
| **VME (Vanilla Milk Expanded)** | `Source/Integrations/VME_Patch/` | `EqualMilking.VME_HarmonyPatch` | `com.akaster.rimworld.mod.equalmilking.vme_harmonypatch` | 分配框、下蛋 Job 等 Patch；独立 Harmony.PatchAll()。 |
| **Dubs Bad Hygiene** | `Source/MilkCum/Milk/Helpers/DubsBadHygieneIntegration.cs` | `MilkCum.Milk.Helpers` | （无独立 Harmony） | 卫生风险系数供乳腺炎等使用；仅静态方法，无 Patch。 |

---

## 二、主 mod Harmony 与手动 Apply

主 mod 使用 `EqualMilkingMod.Harmony`（ID: `com.akaster.rimworld.mod.milkcum`），在 `EqualMilking` 静态构造中：

1. 扫描当前程序集所有带 `[HarmonyPatch]` 的类型并 `PatchClassProcessor` 应用；
2. 再依次调用：
   - `WorkGiver_Ingest_MilkProductFilter.ApplyOptionalPatches(harmony)`
   - `JobDriver_Ingest_MilkProductCheck.ApplyOptionalPatches(harmony)`
   - `ProlactinAddictionPatch.ApplyIfPossible(harmony)`
   - `CumpilationIntegration.ApplyPatches(harmony)`

RJW / PipeSystem / VME 各自持有独立 Harmony 实例，在各自 `[StaticConstructorOnStartup]` 中执行，不经过 `EqualMilking`。

---

## 三、按集成对象的文件清单（便于收口后迁移）

- **RJW**：`Integrations/RJW/` 下 `RJW.cs`, `RJWSexAndFertility.cs`, `RJWLactatingBreastSize.cs`, `RJWLustIntegration.cs`, `RJWVersionDiffHelper.cs`, `Alert_FluidMultiplier.cs`
- **PipeSystem**：`Integrations/PipeSystem/PipeSystem.cs`（单文件含 Patch 与管道注册）
- **VME_Patch**：`Integrations/VME_Patch/VanillaMilkExpandedHarmony.cs`
- **Dubs**：`MilkCum/Milk/Helpers/DubsBadHygieneIntegration.cs`（仍在 Helpers 下）

目录收口已完成：RJW、PipeSystem、VME_Patch 已移入 `Source/Integrations/`，命名空间未改。`MilkCum.csproj` 中 Compile 已改为 `..\Integrations\RJW\**\*.cs`、`..\Integrations\PipeSystem\**\*.cs`。
