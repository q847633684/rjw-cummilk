# Harmony Patch 主题清单

主 mod 与各集成 mod 的 Harmony Patch 按**主题**分类，便于维护与「按主题合并」时参考。  
主 mod 通过程序集扫描应用所有 `[HarmonyPatch]` 类型；本清单仅作主题分类文档，代码中无 Registry。

---

## 一、主 mod（EqualMilkingMod.Harmony）

### 1. Milking（挤奶 / 双池 / 原版替换）

| 类型 | 文件 | 目标 |
|------|------|------|
| `CompMilkable_Patch` | Milking.cs | CompMilkable.Active → 禁用原版 |
| `CompProperties_Milkable_Patch` | Milking.cs | 构造 → compClass 改为 CompEquallyMilkable |
| `ThingWithComps_Patch` | Milking.cs | InitializeComps → 确保 CompEquallyMilkable |
| `Hediff_Pregnant_Patch` | Milking.cs | DoBirthSpawn → 分娩加 Lactating、ApplyBirthPoolValues |
| `Need_Food_Patch` | Milking.cs | 进食相关 |
| `Need_Food_LactatingNutrition_Patch` | Milking.cs | NeedInterval → 泌乳营养 |
| `HediffSet_Remove_Patch` | Milking.cs | RemoveHediff → 清理池缓存等 |
| `FloatMenuMakerMap_Patch` | Milking.cs | 挤奶/吸奶浮窗 |
| `HediffComp_SexPart_CompTipStringExtra_Patch` | Milking.cs | 乳房 tip 显示 |
| `ProlactinAddictionPatch` | Milking.cs | 手动 ApplyIfPossible |

### 2. Breastfeed（哺乳 / 育儿）

| 类型 | 文件 | 目标 |
|------|------|------|
| `FillTabPatch` | Breastfeed.cs | ITab_Pawn_Feeding.FillTab（Transpiler） |
| `ITab_Pawn_Feeding_Patch` | Breastfeed.cs | 自动喂奶选项等 |
| `Pawn_MindState_Patch` | Breastfeed.cs | AutofeedSetting / SetAutofeeder |
| `ChildcareUtility_Patch` | Breastfeed.cs | 哺乳逻辑 |
| `JobDriver_Breastfeed_Patch` | Breastfeed.cs | 哺乳 Job |
| `WorkGiver_Breastfeed_Patch` | Breastfeed.cs | 哺乳 WorkGiver |
| `HediffComp_Chargeable_Patch` | Breastfeed.cs | 充能式吸奶 |

### 3. Compatibility / UI / 其它

| 类型 | 文件 | 目标 |
|------|------|------|
| `StatWorker_ShouldShowFor_Patch` | StatWorker_ShouldShowFor_Patch.cs | StatWorker.ShouldShowFor |
| （手动 Apply） | MilkProductConsumptionPatch.cs | 进食校验 CompShowProducer |
| （手动 Apply） | RecipeProductProducerPatch.cs | 配方产物产主 |
| （手动 Apply） | CumpilationIntegration.cs | 精液桶、Gathering、MakeThing 等 |
| Fixes / EventTrigger / Compatibility | Fixes.cs, EventTrigger.cs, Compatibility.cs | 各兼容与修复 |

---

## 二、RJW（独立 Harmony：com.akaster.rimworld.mod.equalmilking.rjw）

仅显式 Patch 以下两类，不 PatchAll：

| 类型 | 文件 | 目标 |
|------|------|------|
| `CompAssignableToPawn_Box_Patch` | RJW.cs | PawnMilkPoolExtensions.MilkAmount / MilkDef（奶量、奶 Def） |
| `Hediff_BasePregnancy_Patch` | RJW.cs | Hediff_BasePregnancy.PostBirth（RJW 分娩） |

同程序集内其它 RJW 相关 Patch（如 JobDriver_Sex_End_Patch、Game_FinalizeInit_Patch 等）由**主 mod 程序集扫描**应用至主 Harmony，未用 RJW 独立 Harmony。

---

## 三、VME（独立 Harmony：com.akaster.rimworld.mod.equalmilking.vme_harmonypatch）

| 类型 | 文件 | 目标 |
|------|------|------|
| `CompAssignableToPawn_Box_Patch` | VanillaMilkExpandedHarmony.cs | 分配候选等 |
| `JobGiver_LayMilk_Patch` | VanillaMilkExpandedHarmony.cs | 下蛋 Job |

---

## 四、PipeSystem（独立 Harmony：com.akaster.rimworld.mod.equalmilking.pipe）

通过 `Harmony.PatchAll()` 应用本程序集中该命名空间下所有带 `[HarmonyPatch]` 的类（若有）；主要逻辑为管道/龙头注册与事件，非 Patch 类数量少。

---

## 五、统一注册入口（可选实现）

（代码侧未使用 Registry，仅文档按主题分类。）
