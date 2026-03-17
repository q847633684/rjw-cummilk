# RJW 乳房定义参数与 rjw-cummilk 使用说明

本文档整理 RimJobWorld (RJW) 1.6 中与乳房相关的 Def/API 参数，并标注 **rjw-cummilk（Equal Milking）** 使用了其中哪些、用于什么功能。

---

## 一、RJW 乳房相关定义总览

### 1. HediffDef_SexPart（乳房 Hediff 定义）

**RJW 路径**：`rjw/1.6/Defs/HediffDefs/Hediffs_PrivateParts/`  
- 抽象父 Def：`Hediffs_PrivateParts.xml` → `NaturalPrivatePartBreast`  
- 人类具体 Def：`Hediffs_PrivateParts_Human.xml` → `Breasts`

| 参数 | 类型 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|------|------|------|--------------------------|----------|
| fluid | SexFluidDef | 液体类型（乳房为 Milk） | 未直接读 Def | 通过 `ISexPartHediff.GetPartComp().Fluid?.consumable` 判断奶制品类型（RJW.cs Patch） |
| **fluidMultiplier** | float | 产奶/液体量乘数 | **✅ 使用** | **乳池流速倍率**：`PawnMilkPoolExtensions` 中 `(h.def is HediffDef_SexPart d) ? Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f) : 1f`，用于每对乳房的 `FluidPoolEntry.FlowMultiplier` 与左右流速；RJW.cs 中 `MilkAmount` 用 `GetPartComp().FluidMultiplier` 汇总后乘到产奶量 |
| **produceFluidOnOrgasm** | bool | 高潮产液 | **✅ 使用** | **高潮时向乳池追加奶量**：`JobDriver_Sex_OrgasmMilk_Patch` 在 `JobDriver_Sex.Orgasm()` 后，若该角色泌乳且乳房 Def 为 `produceFluidOnOrgasm == true`，则向该对左右池各追加 0.05 池单位奶量（可视为「高潮泌乳」联动）。RJW 默认人类乳房未勾选，子 mod 或 Def 可自行开启 |
| defaultBodyPart | string | 默认部位（Chest） | 未使用 | - |
| sizeMultiplier | float | 随机体型乘数 | 未使用 | - |
| sizeProfile | PartSizeConfigDef | 体型配置（DefaultBreastSizes） | **✅ 使用** | 仅读其 **density**：仅在「奶量增加」时使用（泌乳进水、高潮产液），见下方 PartSizeConfigDef；容量与流速倍率不乘 density |
| partTags / genitalFamily / genitalTags | - | 家族 Breasts、CanLactate 等 | **✅ 使用** | **健康页 Tooltip**：`MilkingPatch` 中 `HediffComp_SexPart.Def.genitalFamily != GenitalFamily.Breasts` 时直接 return，仅对「乳房行」追加奶池块 |
| stages | List\<HediffStage\> | 体型阶段（Nipples→…→Astronomical） | 未使用 | 本 mod 不解析阶段标签，仅用 severity 数值 |

**运行时 Hediff 实例**（Verse.Hediff）：

| 成员 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|------|------|--------------------------|----------|
| **Severity** | 体型严重度（0.01～2+） | **✅ 使用** | **乳池容量**：`cap = Clamp(h.Severity * rjwBreastCapacityCoefficient, 0, 10)`，每对乳房左右池容量均据此计算；**泌乳期临时放大**：`RJWLactatingBreastSizeGameComponent` 在泌乳中为乳房 Hediff 临时 +0.15 severity，结束后恢复 |
| **Part** (BodyPartRecord) | 挂载身体部位 | **✅ 使用** | 池 key 的 fallback（`part.def.defName`）、`GetPartForPoolKey`、乳腺炎按部位判定 |
| **def** | HediffDef | **✅ 使用** | 判断 `HediffDef_SexPart` 取 fluidMultiplier，以及 def.defName 做池 key |

---

### 2. PartSizeConfigDef（乳房体型配置）

**RJW 路径**：`rjw/1.6/Defs/ConfigDefs/PartSizeConfigs.xml` → `DefaultBreastSizes`

| 参数 | 类型 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|------|------|------|--------------------------|----------|
| bodysizescale | bool | 是否按 bodySize 缩放 | 未使用 | - |
| **density** | float? | 密度（重量用） | **✅ 使用** | **仅用于「奶量增加」时放大进池量**：`GetBreastDensity(def)` 读取 `sizeProfile.density`，未配置按 1，限制 0.5～2。泌乳进水（Inflow）中每侧进池量 × 该侧 `FluidPoolEntry.Density`；高潮产液（Orgasm 补丁）追加量 = 基础量 × density。容量与流速倍率不乘 density，由 RJW 的 Severity/fluidMultiplier 决定，density 只影响「同样条件下多进多少奶」 |
| lengths / girths | List\<float\> | 阴茎等用 | 未使用 | - |
| cupSizes | List\<float\> | 罩杯索引曲线 | 未使用 | 本 mod 不显示罩杯/重量，不调用 PartSizeCalculator |

---

### 3. BraSizeConfigDef / BreastSize 结构体

**RJW 路径**：`rjw/1.6/Defs/ConfigDefs/CupSizes.xml`（bandSizeBase、cupSizeLabels）

| 参数/成员 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|-----------|------|--------------------------|----------|
| bandSizeBase | 底围基准 | 未使用 | - |
| cupSizeLabels | 罩杯字母列表 | 未使用 | - |
| BreastSize (cupSize, bandSize, volume, weight) | 运行时计算出的罩杯/体积/重量 | 未使用 | - |

---

### 4. RJW 提供的 API（Pawn / 部位列表）

| API | 返回类型 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|-----|----------|------|--------------------------|----------|
| **pawn.GetBreasts()** | IEnumerable\<ISexPartHediff\> | 1.5：GetSexablePawnParts().Breasts；1.6：GetLewdParts().Breasts.Select(b => b.SexPart) | **✅ 使用** | `RJWVersionDiffHelper.GetBreasts`；用于 RJW.cs 的 MilkAmount/MilkDef Patch、RJWLactatingBreastSize 的泌乳期 Severity 增益 |
| **pawn.GetBreastList()** | List\<Hediff\> | RJW PawnExtensions：RJW 乳房 Hediff 列表（按胸部 BPR 取） | **✅ 使用** | **乳池与容量/流速的核心数据源**：`PawnMilkPoolExtensions` 中所有「按对」逻辑（虚拟左右池、池条目、流速倍率、池 key、身体部位）均基于 GetBreastList()；无乳房时无乳池 |
| HediffComp_SexPart.Def / .parent | Def 为 HediffDef_SexPart，parent 为 Hediff | 健康页性器官行的 Comp | **✅ 使用** | `MilkingPatch` 中仅当 `Def.genitalFamily == Breasts` 时在 Tooltip 后追加奶池块；parent 用于取 pawn、LabelCap、GetPoolKeyForBreastHediff |
| ISexPartHediff.GetPartComp() | HediffComp_SexPart | 取 Comp 的 Fluid、FluidMultiplier | **✅ 使用** | RJW.cs：MilkAmount 用 FluidMultiplier 汇总；MilkDef 用 Fluid.consumable 判断非默认奶制品 |

---

## 二、rjw-cummilk 本 mod 设置（与 RJW 乳房联动）

| 设置项 | 类型 | 说明 | 影响的 RJW 相关逻辑 |
|--------|------|------|----------------------|
| **rjwBreastSizeEnabled** | bool | 是否启用「按 RJW 乳房体型参与乳池/容量/流速」 | 关闭后 GetVirtualBreastPools、GetBreastCapacityFactors、GetBreastPoolEntries、GetMilkFlowMultipliersFromRJW 等直接 return 或返回空；RJWLactatingBreastSize 的 Severity 增益也不执行 |
| **rjwBreastCapacityCoefficient** | float | 乳池容量系数，默认 2 | 每对乳房左右池容量 = `Clamp(hediff.Severity * 本系数, 0, 10)` |

---

## 三、rjw-cummilk 中涉及 RJW 乳房的主要代码位置

| 文件 | 用途简述 |
|------|----------|
| `RJWVersionDiffHelper.cs` | 封装 `GetBreasts()`，兼容 RJW 1.5/1.6 |
| `PawnMilkPoolExtensions.cs` | 用 `GetBreastList()`、`HediffDef_SexPart.fluidMultiplier`、`Severity` 计算虚拟左右池容量、池条目、流速倍率、池 key、身体部位 |
| `RJW.cs` | Harmony Patch：MilkAmount 按乳房 FluidMultiplier 乘算；MilkDef 按 Fluid.consumable 取奶制品 Def |
| `RJWLactatingBreastSize.cs` | 泌乳期对 `GetBreasts()` 的每个乳房 Hediff 临时增加 Severity，结束后恢复 |
| `MilkingPatch.cs` (HediffComp_SexPart_CompTipStringExtra_Patch) | 仅当 `genitalFamily == Breasts` 时在健康页乳房行 Tooltip 后追加奶池块 |
| `MilkCumSettings.cs` | `rjwBreastSizeEnabled`、`rjwBreastCapacityCoefficient` 定义与存档 |
| `Widget_AdvancedSettings.cs` | 高级设置中「RJW 乳房体型」开关与容量系数滑块 |

---

## 四、RJW 乳房体型：因素与处理方式

RJW 的「乳房大小」由以下因素决定，并在 **HediffComp_SexPart** 中统一处理：

| 因素 | 说明 | 处理方式 |
|------|------|----------|
| **baseSize** | 绝对体型（Comp 内字段，会存档） | 初始化时 `baseSize = Def.GetRandomSize() * bodySize`；`GetRandomSize()` 为 Def 内高斯随机 × sizeMultiplier，Clamp 到 0.01～1。男性 trap 乳房多为 0.01。 |
| **forcedSize** | 可选强制体型（如手术/脚本锁定） | 若存在则 `GetSize()` 返回 forcedSize，否则返回 baseSize；`SetSeverity` 在存在 forcedSize 时**不会**改写 baseSize，只做 SyncSeverity。 |
| **Pawn.BodySize** | 种族体型 | `GetSeverity() = GetSize() / Pawn.BodySize`，即 Severity 为「相对体型」；`SetSeverity(sev)` 时 `baseSize = sev * Pawn.BodySize`。 |
| **SyncSeverity** | 与 Hediff 显示一致 | `SyncSeverity()` 将 `parent.Severity = GetSeverity()`，RJW 多处会调 `UpdateSeverity()`/`SyncSeverity()`，因此**只改 parent.Severity 会被覆盖**，必须通过 `SetSeverity` 改 baseSize。 |

因此本 mod 泌乳期临时加大乳房应使用 **Comp.SetSeverity() / GetSeverity()**，才能与 RJW 的 baseSize/Sync 一致；仅改 `hediff.Severity` 会被后续 Sync 覆盖。

**存读档**：已在 `RJWLactatingBreastSizeGameComponent.ExposeData` 中持久化：存档时将「施加前 Severity」按 (Pawn GetUniqueLoadID, GetBreastList 序号) 写入 `_pendingRestore`，读档后在每次 Tick 中遇到对应 Pawn 时解析回 `BreastBaseSeverity` 字典，避免读档后重复施加。

---

## 五、撑大/缩小乳房与「读 vs 写」：应统一用 RJW 定义并对其赋值

### 5.1 结论（简短）

- **撑大/缩小乳房**：必须用 RJW 的**写接口**达成，即 **HediffComp_SexPart.SetSeverity(severity)**（或 SetSeverity 内部更新的 baseSize），不能在本 mod 里自建一套“乳房大小”再只读 RJW。
- **BreastSize（罩杯/体积/重量）**：在 RJW 里是**只读**的派生值，由 `PartSizeCalculator.TryGetBreastSize(hediff)` 根据当前 (def, severity, bodySize) 和曲线**计算**得出，**没有**对 BreastSize 或罩杯/体积/重量直接赋值的 API。要改变“罩杯/体积/重量”的显示或效果，只能通过改 **Severity**（即 SetSeverity）间接实现。
- **若希望按「目标罩杯/体积/重量」撑大或缩小**：应用 RJW 的 **PartSizeCalculator.Inversion.TryInvertBreastVolume(hediffDef, volume, bodySize, out severity, out _)** 或 **TryInvertBreastWeight** 得到对应的 severity，再用 **comp.SetSeverity(severity)** 赋值。这样效果与 RJW 的 Def/曲线一致。
- **其他泌乳逻辑**：容量、流速、进水等目前只**读** RJW 的 Severity/fluidMultiplier/density，没有写 RJW 乳房状态。若未来有「因泌乳而永久撑大/缩小」等需要改写体型的地方，也应通过 **SetSeverity**（或 TryInvert + SetSeverity）对 RJW 赋值，而不是在本 mod 内单独维护一套数字。

### 5.2 本 mod 当前用法汇总

| 场景 | 读 / 写 | 使用 RJW 的什么 | 是否对 RJW 赋值 |
|------|--------|------------------|------------------|
| 乳池容量、流速、池条目 | 只读 | Hediff.Severity、def.fluidMultiplier、sizeProfile.density | 否 |
| 泌乳期临时加大乳房 | 先读后写 | Comp.GetSeverity()、Comp.SetSeverity() | 是（SetSeverity(orig+0.15) / SetSeverity(baseSev)） |
| 高潮产液、MilkAmount/MilkDef Patch | 只读 | GetBreasts()、FluidMultiplier、Fluid.consumable 等 | 否 |
| 未来若有「按体积/罩杯撑大」 | 写 | TryInvertBreastVolume/Weight → severity，再 SetSeverity(severity) | 是（应通过 RJW 的 Inversion + SetSeverity 实现） |

### 5.3 为何用 Severity 而不是直接写 BreastSize

- RJW 的**唯一可写体型来源**是 **HediffComp_SexPart.baseSize**（以及可选的 forcedSize）；对外统一用 **Severity**（= GetSize()/BodySize）和 **SetSeverity** 读写。
- **BreastSize**（cupSize、bandSize、volume、weight）由 PartSizeCalculator 根据 **severity + def + bodySize** 现场算出来，仅用于显示和兼容（如 CompRJWThingBodyPart 按体积反算 severity 再 SetSeverity）。因此本 mod 的「撑大/缩小」应统一为：**对 RJW 的 Severity 赋值（SetSeverity）**；若需求是「按罩杯/体积/重量」改，则用 **TryInvertBreastVolume/Weight → 再 SetSeverity**，而不是对 BreastSize 或罩杯/体积/重量赋值（RJW 无此类 API）。

---

## 六、设计原则：不重复计算 RJW 乳房体型

RJW 已完整定义并维护「乳房大小」：

- **HediffComp_SexPart**：存 `baseSize`，`GetSeverity() = GetSize() / Pawn.BodySize`，并把结果同步到 `Hediff.Severity`。  
- **PartSizeCalculator**：在需要罩杯/底围/体积/重量时，用 `(def, severity, bodySize)` 和 sizeProfile 曲线现场计算。

因此 **体型（bodySize）、阶段、曲线等都已由 RJW 算好并体现在 Severity 上**。  
本 mod 只使用 RJW 的**结果**：`Hediff.Severity`、`def.fluidMultiplier`、`sizeProfile.density`，**不再自行乘 BodySize 或调用 PartSizeCalculator**，避免重复计算与逻辑不一致。

---

## 七、未使用的 RJW 乳房相关项（摘要）

- **PartSizeConfigDef**：cupSizes、bodysizescale 未使用；**density 已接入**（见上）。  
- **BraSizeConfigDef / BreastSize**：罩杯字母、底围、体积、重量均未使用。  
- **PartSizeCalculator**：TryGetBreastSize、TryGetCupSize 等均未调用。  
- **HediffDef_SexPart**：fluid、stages、defaultBodyPart、sizeMultiplier 未直接参与本 mod 的乳池或 UI 逻辑（fluid 通过 Comp 的 Fluid 间接用于奶制品类型；sizeProfile.density 已用于奶量增加倍率）。

---

*文档版本：基于 RJW 1.6 与 rjw-cummilk 当前源码整理。*
