# rjw-cummilk 与游戏本体及 RJW 的冲突与优化总结

本文档总结当前代码/逻辑与 **RimWorld 原版**、**RJW** 之间可能存在的**冲突点**、**可合并点**，以及**可优化的调用关系**。与 [游戏已接管变量与机制清单](游戏已接管变量与机制清单.md)、[泌乳系统逻辑图](泌乳系统逻辑图.md) 配合使用。

---

## 设计 / 落地原则（核对清单）

实现与重构时建议遵守以下原则，与「只读、只挂接」一致：

1. **耐受 / 成瘾：交给原版**  
   成瘾判定与概率、耐受增减由原版 ChemicalDef + CompProperties_Drug + outcomeDoers/SeverityPerDay 驱动；本 mod 只读耐受 t 并换算 E_tol，不手写成瘾概率或耐受公式。

2. **分娩入口统一**  
   原版 `Hediff_Pregnant.DoBirthSpawn` 与 RJW `Hediff_BasePregnancy.PostBirth` 两处均挂接，统一在「出生时」调用 `PoolModelBirthHelper.ApplyBirthPoolValues(mother)`，不重复实现分娩逻辑。

3. **不重复定义「种族是否产奶」**  
   不在 C# 里定义底层规则；仅用 `namesToProducts` 与设置做开关层，人形/动物沿用原版 CompMilkable 有无 + 本 mod 设置。

4. **只读原版/Def 数据**  
   耐受 t、成瘾状态、Lactating severity、RaceProps、体型、BodyResourceGrowthSpeed、StatDef（如 AnimalGatherYield）等均由原版或 Def 提供，本 mod 只读并换算为池、E_tol、流速等，不重写原版公式。

5. **Lactating 阶段：XML 移除 + C# 动态**  
   stages 由 XML 移除，C# 动态 `GenStage()`；capMods 在动态阶段里设置，避免与 Def 静态阶段冲突。

6. **Need_Food：仅 Postfix 追加**  
   仅 Postfix 增加「泌乳额外扣饱食度」与「回缩吸收加回」，不替换原版 NeedInterval 主逻辑。

7. **与 RJW / 原版：仅挂接、不改写内部**  
   对 RJW 仅使用其乳房/分娩 API（如 GetBreastList、PostBirth）；对原版仅替换 class（Lactating/Job/WorkGiver/Comp）或 Postfix 追加行为；关键补丁用 HarmonyPriority 控制顺序，避免覆盖其它 mod。

---

## 一、潜在冲突点

### 1.1 与原版 Def / 运行时的「接管」冲突

| 项目 | 当前做法 | 冲突风险 |
|------|----------|----------|
| **HediffDefOf.Lactating** | 运行时 `hediffClass = HediffWithComps_EqualMilkingLactating`；XML Patch 替换 comp、移除 stages/SeverityPerDay；`maxSeverity = 100f` 在 `UpdateEqualMilkingSettings` 中设置。 | 若其他 mod 在 Def 加载后再次修改 Lactating（如改 hediffClass、maxSeverity），加载顺序会导致一方被覆盖。本 mod 在 LongEvent/PostLoad 里写 Def，应尽量晚执行。 |
| **JobDefOf.Milk** | `driverClass = JobDriver_EquallyMilk`（在 `EqualMilking.Init()`）。 | 仅本 mod 替换 driver；若另有 mod 也改同一 JobDef，后执行者生效。 |
| **WorkGiverDef "Milk"** | `giverClass = WorkGiver_EquallyMilk`。 | 同上。 |
| **CompProperties_Milkable** | Harmony Postfix 构造时 `compClass = CompEquallyMilkable`。 | 对所有带 CompProperties_Milkable 的种族生效；若某 mod 依赖「原版 CompMilkable 行为」会失效（本 mod 已用 Prefix 将 `CompMilkable.Active` 恒返 false，避免原版逻辑跑）。 |

**建议**：保持现有「仅替换 class、不新增 Def」的策略；若出现与其它 mod 冲突，可考虑用 HarmonyPriority 或条件 patch（如仅当无其它 lactation mod 时再替换）。

### 1.2 与原版/其它 mod 的 Harmony 目标重叠

本 mod 对以下类型/方法打了补丁，若其它 mod 也 patch 同一目标，存在执行顺序与互相覆盖问题：

| 目标 | 用途 |
|------|------|
| `CompMilkable.Active` getter | Prefix 强制返回 false（已用 `[HarmonyPriority(Priority.First)]`） |
| `CompProperties_Milkable` 构造 | Postfix 替换 compClass |
| `ThingWithComps.InitializeComps` | Postfix 确保 Pawn 有 CompEquallyMilkable |
| `Hediff_Pregnant.DoBirthSpawn` | Prefix 为人/动物分娩加 Lactating + 水池增量 |
| `Need_Food.GetTipString` / `NeedInterval` | 悬停显示饥饿率、泌乳额外扣饱食度与回缩吸收 |
| `Pawn_HealthTracker.RemoveHediff` | Postfix：Lactating 被移除时清双池 |
| `RaceProperties.SpecialDisplayStats` | 替换/追加产奶统计 |
| `FloatMenuMakerMap.AddHumanlikeOrders` | 追加挤奶/吸奶等右键菜单 |
| `AnimalProductionUtility` 迭代器 | Transpiler：GetCompProperties&lt;CompProperties_Milkable&gt; 改为返回 null，隐藏原版产奶行 |
| `HealthCardUtility.GetTooltip` | Postfix 追加乳房池悬停 |
| `IngestionOutcomeDoer.DoIngestionOutcome` | Postfix：催乳剂服用后移除 Lactating、入队延迟、AddFromDrug |

**建议**：关键补丁已用注释标明用途；若与其它 mod 冲突，可对特定补丁设 `Priority` 或 `before`/`after`，或做运行时存在性检查再 patch。

### 1.3 与 RJW 的依赖与版本

| 项目 | 说明 |
|------|------|
| **RJW 程序集** | `Source/RJW/RJW.cs` 中 `ApplyPatches` 依赖 `AccessTools.TypeByName("SexFluidDef")`，若 RJW 版本过旧则中止 patch，避免崩溃。 |
| **RJW 分娩路径** | 原版 Biotech 走 `Hediff_Pregnant.DoBirthSpawn`；RJW 人形怀孕走 `Hediff_BasePregnancy` → `PostBirth(mother, father, baby)`。本 mod 已 patch **原版** `DoBirthSpawn`，并**单独** patch **RJW** `Hediff_BasePregnancy.PostBirth`（Prefix），在 PostBirth 内为母亲加 Lactating/刷新 Severity 并调用水池逻辑。两路径并存，无重复添加。 |
| **RJW Genital_Helper** | `ExtensionHelper.GetGenitalsBPR` 使用 `rjw.Genital_Helper.get_genitalsBPR(pawn)`，RJW 未加载或 API 变化会异常，当前用 try 或条件调用可降低风险。 |

**结论**：RJW 分娩已双路径挂接；唯一需注意的是 RJW 大版本更新时 `PostBirth` 签名或 `SexFluidDef` 是否存在。

### 1.4 信息面板与「游戏理解」不一致

- **药物浓度/有效浓度时间**：原版用「主效果 Hediff 的 Severity + SeverityPerDay」算；本 mod 主效果为 Lactating，但**真实持续时间由池 L 与 200 tick 衰减**决定，Lactating 无 SeverityPerDay，故面板显示与池模型不一致（文档 6.2 已说明，属设计取舍）。
- **能力影响**：药品信息从 Hediff 的 stages.capMods 读；Lactating 的 stage 在 C# 里动态生成且 capMods 被 Clear，故药品「能力影响」不列泌乳增益；增益改由独立 Hediff `EM_LactatingGain` 提供，仅健康页与实际能力生效。

无逻辑冲突，仅表现上与「原版成瘾品区块」不完全一致。

---

## 二、可合并点

### 2.1 耐受 / 成瘾：尽量交给原版

| 当前做法 | 建议 |
|----------|------|
| 耐受数值由 XML outcomeDoers 给 EM_Prolactin_Tolerance + HediffCompProperties_SeverityPerDay -0.015。 | 若确认原版在服用带 chemical 的药物时**已自动**按 ChemicalDef 增加 toleranceHediff 严重度，可考虑去掉 outcomeDoers 中**单独** GiveHediff(EM_Prolactin_Tolerance)，只保留 SeverityPerDay 做自然恢复；否则保留现状。 |
| 成瘾判定与概率 | 文档建议交给原版 ChemicalDef + CompProperties_Drug；**当前 C# 未手写成瘾概率**（无 HandleAddictionMechanics 式逻辑），仅 DoIngestionOutcome_Postfix 做「移除 Lactating、算 t_before、入队/立即 AddFromDrug」。可保持「成瘾由游戏驱动、mod 只读」的设计。 |
| 有效药效 E_tol | 已收束到 `GetProlactinToleranceFactor(pawn)` 单点读取耐受并换算，无多处手写。 |

**只读游戏收束**：若经 Profiler 或小版本验证确认，原版在服用带 chemical 的药物时**已自动**按 ChemicalDef 增加 toleranceHediff 严重度，可考虑在 outcomeDoers 中**去掉**单独 `GiveHediff(EM_Prolactin_Tolerance)`，只保留 HediffDef 的 SeverityPerDay 做自然恢复；否则保留现状，不重复造轮子。

### 2.2 分娩入口统一（概念合并）

- **原版**：`Hediff_Pregnant.DoBirthSpawn` → 本 mod Prefix 加 Lactating + `PoolModelBirthHelper.ApplyBirthPoolValues(mother)`。
- **RJW**：`Hediff_BasePregnancy.PostBirth` → 本 mod Prefix 加 Lactating（若可挤奶）+ **已统一调用** `ApplyBirthPoolValues(mother)`（与原版一致）。

两处均已挂接并统一调用 `ApplyBirthPoolValues`，逻辑为「分娩/出生时统一调用 ApplyBirthPoolValues」。

### 2.3 「谁可挤奶」与设置层

- 人形：沿用原版逻辑（CompProperties_Milkable 有无 + 本 mod 设置开关）。
- 动物：原版 CompMilkable + 本 mod 替换为 CompEquallyMilkable，设置项控制是否启用。
- 不在 C# 里重复定义「种族是否产奶」的底层规则，仅用 `namesToProducts` 与设置做开关层，与文档「只读、只挂接」一致。

---

## 三、可优化的调用关系

### 3.1 与游戏本体

- **只读游戏**：耐受 t、成瘾状态、Lactating severity、RaceProps、体型、BodyResourceGrowthSpeed、StatDef（如 AnimalGatherYield）等均由原版或 Def 提供，本 mod 只读并换算为池、E_tol、流速等，不重写原版公式。
- **Lactating 阶段**：stages 由 XML 移除，C# 动态 `GenStage()`；capMods 在动态阶段里设置，避免与 Def 静态阶段冲突。
- **Need_Food**：仅 Postfix 增加「泌乳额外扣饱食度」与「回缩吸收加回」，不替换原版 NeedInterval 主逻辑。

### 3.2 与 RJW

- **乳房/胸部**：容量与流速使用 RJW HediffDef_SexPart（fluidMultiplier）、GetBreastList 等；未启用 rjwBreastSizeEnabled 时退化为对称双池（0.5+0.5）。若 RJW 未来提供统一「可挤奶/产奶」接口，可优先调用，减少直接依赖其 Hediff 结构。
- **分娩**：已通过 **Hediff_BasePregnancy.PostBirth** 挂接，无需修改 RJW 内部；RJW 出生时加 Lactating + 刷新 Severity 后**统一调用 ApplyBirthPoolValues(mother)**，与原版分娩一致。
- **RJW 联动设置**：rjwLustFromNursingEnabled、rjwSexNeedLactatingBonusEnabled、rjwLactatingInSexDescriptionEnabled 等均由本 mod 设置控制，不反向依赖 RJW 内部默认值，调用关系清晰。

### 3.3 与 rjw-genes

- 流速倍率通过 `GetMilkFlowMultipliersFromRJW` 等与基因（如 rjw_genes_big_breasts）兼容；基因倍率在 `ExtensionHelper` 中集中读取，无重复逻辑。

### 3.4 补丁顺序与优先级

- 关键替换类补丁（如 `CompMilkable.Active`）已用 `HarmonyPriority(Priority.First)`，减少被其它 mod 覆盖导致原版挤奶重新生效的风险。
- 其它若与已知 mod 冲突，可对单个 patch 设 `before`/`after` 或 `Priority`，避免全局提高优先级。

---

## 四、小结表

| 类别 | 要点 |
|------|------|
| **冲突** | 原版 Lactating/Job/Milk/CompMilkable 被本 mod 接管 → 与同改这些 Def 的 mod 可能顺序冲突；多处 Harmony 与原版/RJW 共享目标，需注意加载顺序与优先级；RJW 版本过旧会禁用 RJW 补丁。 |
| **合并** | 耐受/成瘾尽量交给原版 Def；E_tol 已单点；分娩原版+RJW 双路径均已挂接，可视为统一「出生时 ApplyBirthPoolValues」；谁可挤奶不重复定义底层规则。 |
| **优化** | 坚持「只读游戏」；RJW 仅用其乳房/分娩 API，若以后有统一接口可收敛；补丁优先级在关键处已设，其余按需细调。 |

与《游戏已接管变量与机制清单》《耐受系统重构设计》一致：**以「耐受与成瘾由游戏定义与驱动，本 mod 只读并换算为水池与 UI 所需系数」为目标**，即可最大程度减少冲突并保持可维护性。

---

## 五、实现说明（设计原则落地）

| 原则 | 实现情况 |
|------|----------|
| 1 耐受/成瘾交给原版 | C# 无手写成瘾概率；`ProlactinAddictionPatch` 仅 Postfix 挂接服用后的水池逻辑（移除 Lactating、入队延迟、AddFromDrug）；耐受/成瘾由 XML Def 驱动，代码只读 t 并算 E_tol。 |
| 2 分娩入口统一 | 原版 `Hediff_Pregnant.DoBirthSpawn` 与 RJW `Hediff_BasePregnancy.PostBirth` 两处均调用 `PoolModelBirthHelper.ApplyBirthPoolValues(mother)`；RJW 侧已在 PostBirth_Prefix 中补充该调用。 |
| 3 不重复定义种族产奶 | `UpdateEqualMilkableComp` / `GetDefaultMilkProduct` 仅用 Def 的 race.Humanlike/体型作默认，实际开关由 `namesToProducts` 与设置决定。 |
| 4 只读原版/Def | 耐受、成瘾、Lactating severity、RaceProps、体型、StatDef 等仅读并换算，未重写原版公式；`GetDefaultMilkProduct` 注释已标明。 |
| 5 Lactating 阶段 | `Patches/LactatingPatch.xml` 移除 stages；`HediffWithComps_EqualMilkingLactating.GenStages()` 动态生成，capMods 在 GenStage 内处理。 |
| 6 Need_Food 仅 Postfix | `Need_NeedInterval_Patch` 仅 Postfix 追加泌乳额外扣饱食度与回缩吸收，未替换 NeedInterval 主逻辑。 |
| 7 仅挂接 + 优先级 | 分娩/服用等为 Prefix 或 Postfix 挂接；`CompMilkable.Active` 已用 `[HarmonyPriority(Priority.First)]`；关键补丁注释标明设计原则。 |

