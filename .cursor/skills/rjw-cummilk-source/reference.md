# rjw-cummilk Source 参考

## 命名空间与目录对应

| 目录 (Source 下) | 命名空间 |
|------------------|----------|
| MilkCum/Core | MilkCum.Core |
| MilkCum/UI | MilkCum.UI |
| MilkCum/Milk/Comps | MilkCum.Milk.Comps |
| MilkCum/Milk/Data | MilkCum.Milk.Data |
| MilkCum/Milk/Givers | MilkCum.Milk.Givers |
| MilkCum/Milk/Jobs | MilkCum.Milk.Jobs |
| MilkCum/Milk/Helpers | MilkCum.Milk.Helpers |
| MilkCum/Milk/Thoughts | MilkCum.Milk.Thoughts |
| MilkCum/Milk/HarmonyPatches | MilkCum.Milk.HarmonyPatches |
| RJW | MilkCum.RJW |
| PipeSystem | MilkCum.PipeSystem |

## EMDefOf 常用项（示例）

- ThingDef: EM_Prolactin, EM_Lucilactin, EM_HumanMilk, EM_MilkingPump, EM_MilkingElectric
- HediffDef: EM_Prolactin_Tolerance, EM_Prolactin_Addiction, EM_Prolactin_High, EM_Mastitis, EM_BreastsEngorged, EM_LactatingGain, EM_AbsorptionDelay
- JobDef: EM_InjectLactatingDrug, EM_ForcedBreastfeed, EM_ActiveSuckle
- WorkGiverDef: EM_MilkEntity
- ChemicalDef: EM_Prolactin_Chemical；NeedDef: Chemical_EM_Prolactin
- ThoughtDef: EM_Prolactin_Joy, EM_Prolactin_Withdrawal, EM_ForcedMilking, EM_MilkPoolFull 等
- PawnTableDef / PawnColumnDef: Milk_PawnTable, Milk_Lactating, Milk_Fullness, Milk_RemainingDays, Milk_MainButton
- StatDef: EM_Milk_Amount_Factor, EM_Lactating_Efficiency_Factor

原版常用：HediffDefOf.Lactating, JobDefOf.Milk, JobDefOf.Breastfeed, JobDefOf.BabySuckle, ThingDefOf.Human, RaceProps.Humanlike。

## 翻译与 Lang

- Keyed: `"Equal_Milking".Translate()`、`"EM.LactatingGainPercent".Translate(value)`；占位符 {0} 与参数顺序一致。
- Def 默认文案：`def.SetDefaultLabel(text)`、`def.SetDefaultDesc(text)`（Lang 扩展）；若 DefInjected 已存在则跳过。
- 组合词：`Lang.Join(Lang.Milk, Lang.Pump)` 等；原版词可用 `"Milk".DefLabel<ThingDef>()`、`HediffDefOf.Lactating.label`。
- 新增 Keyed 键：在 Languages/English/Keyed 与 Languages/ChineseSimplified/Keyed 中同时添加同名键。

## 设置与 Scribe 键

- 存档键前缀：`"EM.XXX"`（如 `"EM.MaxLactationStacks"`、`"EM.AllowMastitis"`）；修改会影响存档，需谨慎。
- 委托到子对象时（如 MilkRiskSettings），对外仍用 EqualMilkingSettings 的静态属性，内部 Scribe 可复用子对象序列化。

## Harmony 约定

- 补丁类：`[HarmonyPatch(typeof(CompMilkable))]`、`[HarmonyPatch("Active", MethodType.Getter)]`。
- 方法：`[HarmonyPrefix]` / `[HarmonyPostfix]`，`public static`；返回 bool 的 Prefix 中 `return false` 表示跳过原方法。
- 替换 comp/driver：在 Constructor 或合适入口的 Prefix 中改 `__instance.compClass` / `driverClass` 等并 `return false`（或按需执行原逻辑）。
- 注释注明设计来源（如「规格 7.8」「水池模型」「游戏已接管」）。

## Docs 与逻辑分工

- **游戏已接管**：成瘾/耐受数值、Need 升降、SeverityPerDay、分娩事件、体型/种族判定等由原版或 Def 驱动；C# 只读或挂接。
- **参数联动**：baselineMilkDurationDays、allowMastitis、toleranceFlowImpactExponent 等与《参数联动表》一致；新增参数时在 Docs 中补充。
- **泌乳逻辑**：双乳水位、流速、L 衰减、溢出、选侧、心情联动等与《泌乳系统逻辑图》一致；Docs 索引见 `Docs/README.md`。
