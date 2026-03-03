# [convention] EMDefOf 与核心代码位置

- **type**: convention
- **date**: 2025-03（根据当前代码）
- **tags**: #EMDefOf #命名空间 #代码结构
- **related_docs**: .cursor/skills/rjw-cummilk-source
- **最后与代码核对**: 2025-03

## 约定要点

- **本 mod 的 Def** 一律通过 **EMDefOf**（`MilkCum.Core.EMDefOf`）访问，不手写 defName 字符串重复引用。
- **原版/它 mod Def** 用 `DefOf`（如 `HediffDefOf.Lactating`、`JobDefOf.Milk`）或 `DefDatabase<T>.GetNamed("defName")`；可选 Def 用 `GetNamedSilentFail` 避免未加载时崩溃。

## EMDefOf 当前列表（Source/MilkCum/Core/EMDefOf.cs）

| 类别 | Def 示例 |
|------|----------|
| 物品/建筑 | EM_Prolactin, EM_Lucilactin, EM_HumanMilk, EM_MilkingPump, EM_MilkingElectric |
| Job/WorkGiver | EM_InjectLactatingDrug, EM_ForcedBreastfeed, EM_ActiveSuckle, EM_MilkEntity |
| 基因 | EM_Lactation_Enhanced, EM_Lactation_Poor, EM_Permanent_Lactation |
| UI | Milk_PawnTable, Milk_MilkType, Milk_Lactating, Milk_Fullness, Milk_RemainingDays, Milk_MainButton |
| 成瘾/化学 | EM_Prolactin_Chemical, EM_Prolactin_Tolerance, EM_Prolactin_Addiction, Chemical_EM_Prolactin_Chemical |
| 心情/记忆 | EM_Prolactin_Joy, EM_Prolactin_Withdrawal, EM_ForcedMilking, EM_NursedBy, EM_NursedSomeone, EM_LongTermDrugLactation 等 |
| Hediff | EM_Mastitis, EM_BreastsEngorged, EM_DrugLactationBurden, EM_LactatingGain, EM_AbsorptionDelay |
| 管道（可选） | EM_PipeNetworks, EM_MilkTap, EM_MilkPipe, EM_MilkValve, EM_MilkContainer 等 |

EM_Defaults 不参与 DefOf 绑定，使用处通过 GetNamedSilentFail 获取。

## 核心代码位置（便于检索）

| 职责 | 文件/类 |
|------|----------|
| 水池 L、衰减、R/I | `HediffWithComps_EqualMilkingLactating`（Milk/Comps） |
| 双池进水、DrainForConsume、挤奶/吸奶 | `CompEquallyMilkable`（Milk/Comps） |
| 池条目与按对分组 | `ExtensionHelper.GetBreastPoolEntries`（Milk/Helpers） |
| 双池状态与 TickGrowth | `LactationPoolState`（Milk/Data） |
| 常量 B_T、k、ε、吸收延迟、撑大系数 | `PoolModelConstants`（Milk/Helpers/Constants.cs） |
| 设置与存档 | `EqualMilkingSettings`（Core） |
| 吸收延迟队列 | `WorldComponent_EqualMilkingAbsorptionDelay`（Milk/World） |

命名空间与目录对应关系见 .cursor/skills/rjw-cummilk-source（如 Milk/Comps → MilkCum.Milk.Comps）。
