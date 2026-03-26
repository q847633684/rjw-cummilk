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
| **fluidMultiplier** | float | 产奶/液体量乘数 | **✅ 使用** | **乳池流速倍率**：`RjwBreastPoolEconomy.GetBreastHediffFlowMultiplier`（优先 `HediffComp_SexPart.GetFluidMultiplier()`，与药品 `partFluidMultiplier` 一致），用于 `FluidPoolEntry.FlowMultiplier`；RJW.cs 中 `MilkAmount` 仍用各乳房 `GetPartComp().FluidMultiplier` 汇总 |
| **produceFluidOnOrgasm** | bool | 高潮产液 | **✅ 使用** | **高潮时向乳池追加奶量**：`JobDriver_Sex_OrgasmMilk_Patch` 在 `JobDriver_Sex.Orgasm()` 后，若该角色泌乳且乳房 Def 为 `produceFluidOnOrgasm == true`，则向该对左右池各追加 0.05 池单位奶量（可视为「高潮泌乳」联动）。RJW 默认人类乳房未勾选，子 mod 或 Def 可自行开启 |
| defaultBodyPart | string | 默认部位（Chest） | 未使用 | - |
| sizeMultiplier | float | 随机体型乘数 | 未使用 | - |
| sizeProfile | PartSizeConfigDef | 体型配置（DefaultBreastSizes） | **✅ 使用** | 仅读其 **density**：仅在「奶量增加」时使用（泌乳进水、高潮产液），见下方 PartSizeConfigDef；容量与流速倍率不乘 density |
| partTags / genitalFamily / genitalTags | - | 家族 Breasts、CanLactate 等 | **✅ 使用** | **健康页 Tooltip**：`MilkingPatch` 中 `HediffComp_SexPart.Def.genitalFamily != GenitalFamily.Breasts` 时直接 return，仅对「乳房行」追加奶池块 |
| stages | List\<HediffStage\> | 体型阶段（Nipples→…→Astronomical） | **✅ 可选** | 设置 `rjwNippleStageFlowBonusPercent≠0` 且当前 `CurStage.label` 含 “Nipple” 时，对 `GetBreastHediffFlowMultiplier` 施加约 ±15% 内修饰 |

**运行时 Hediff 实例**（Verse.Hediff）：

| 成员 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|------|------|--------------------------|----------|
| **Severity** | 体型严重度（0.01～2+） | **✅ 使用** | **乳池基容量**：由 `rjwBreastPoolCapacityMode` 在**严重度 / 重量 / 体积**三档中选一路，再 `Clamp(...,0,10)`；**泌乳期临时放大**：`RJWLactatingBreastSizeGameComponent` 按池与 L 驱动 `SetSeverity`，离乳恢复 |
| **Part** (BodyPartRecord) | 挂载身体部位 | **✅ 使用** | 池 key 的 fallback（`part.def.defName`）、`GetPartForPoolKey`、乳腺炎按部位判定 |
| **def** | HediffDef | **✅ 使用** | 判断 `HediffDef_SexPart` 取 fluidMultiplier，以及 def.defName 做池 key |

---

### 2. PartSizeConfigDef（乳房体型配置）

**RJW 路径**：`rjw/1.6/Defs/ConfigDefs/PartSizeConfigs.xml` → `DefaultBreastSizes`

| 参数 | 类型 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|------|------|------|--------------------------|----------|
| bodysizescale | bool | 是否按 bodySize 缩放 | 未使用 | - |
| **density** | float? | 密度（重量用） | **❌ 不直连进池** | RJW 内参与 `BreastSize.weight` 等；**容量模式含「重量」时**经 `TryGetBreastSize` 的 `weight` 间接体现。**泌乳进水、高潮灌池不再乘密度** |
| lengths / girths | List\<float\> | 阴茎等用 | 未使用 | - |
| cupSizes | List\<float\> | 罩杯索引曲线 | **✅ 间接** | 经 `PartSizeCalculator.TryGetBreastSize` 参与 **volume/weight/cup/band**；容量模式为「体积」时使用 `volume` |

---

### 3. BraSizeConfigDef / BreastSize 结构体

**RJW 路径**：`rjw/1.6/Defs/ConfigDefs/CupSizes.xml`（bandSizeBase、cupSizeLabels）

| 参数/成员 | 说明 | **rjw-cummilk 是否使用** | **用途** |
|-----------|------|--------------------------|----------|
| bandSizeBase | 底围基准 | 未使用 | - |
| cupSizeLabels | 罩杯字母列表 | 未使用 | - |
| BreastSize (cupSize, bandSize, volume, weight) | 运行时计算出的罩杯/体积/重量 | **✅** | 快照字段 + 容量：按模式取 `weight` 或 `volume`（纯严重度档不读）；`RJWLactatingBreastSize` 仍只调 Severity，与 RJW API 一致 |

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

**存档策略**：当前版本不再读取已废弃的 Scribe 键（如 `PoolCurrentLactationAmount`、单列 `EM.Inflammation`、`BreastFullnessKeys`/`BreastFullnessValues` 双列表等）；旧档需新开或接受对应字段丢失/回默认。

### 产品句（验收）

**容量与 RJW 展示对齐**：单侧基容量由 `MilkCumSettings.rjwBreastPoolCapacityMode` **三档选一**：纯严重度×系数、纯 RJW 重量×系数、纯 `BreastSize.volume`×系数（新档默认体积），再 `Clamp(...,0,10)`；重量/体积档无有效 RJW 尺寸则该侧为 0、不进虚拟池。**流速**与 RJW 一致走 `HediffComp_SexPart.GetFluidMultiplier()`（及 def 回退），并可选乘乳头阶段小修饰；**组织密度不参与进水/高潮灌池乘数**（容量选重量模式时间接体现）；**L** 仍只表示还能泌多久。

### 阶段 0：读乳房入口与公式映射（维护者表）

| 数值 / 行为 | 唯一公式或入口 | 主要代码 |
|-------------|----------------|----------|
| 池键 `poolKey` | `part.defName` 优先否则 `hediff.defName` + `_` + `GetBreastList` 下标 | `RjwBreastPoolEconomy.BuildPoolKey` |
| 乳房对快照（容量侧、流速、体积/重量/罩杯、Hediff 引用） | 每条参与池的乳房 Hediff 一条 `RjwBreastPairSnapshot` | `RjwBreastPoolEconomy.GetBreastPairSnapshots` |
| 单侧基容量（组织适应前） | 见枚举模式；统一 `Clamp(…,0,10)` | `RjwBreastPoolEconomy.ComputeBaseCapacityPerSide` |
| 虚拟左右池 | **每条**参与池的乳房 Hediff 固定生成 `poolKey_L` 与 `poolKey_R` 两键；多 Hediff 种族亦为每对两键，不合并单池 | `PawnMilkPoolExtensions.GetBreastPoolEntries` |
| 泌乳进水基础日流速（不含侧别） | `drive × BodyResourceGrowthSpeed × defaultFlowMultiplierForHumanlike`（**不再**乘全局 cond） | `CompEquallyMilkable.UpdateMilkPools` |
| 侧别状态对流速 | `GetMilkFlowMultiplierFromConditions(该对乳房 Part)` | `PawnMilkPoolExtensions.GetConditionsForSide` |
| 高潮产液 | 左右各 `0.05×density`（每对乳房 Hediff） | `JobDriver_Sex_OrgasmMilk_Patch` |

### 真实泌乳生理 ↔ 本 mod 分工（验收与后续扩展）

下列对照便于判断「哪些是解剖学意义上的单侧/对侧」，哪些是 **游戏或数据结构的合理简化**。

| 生理/临床概念 | 更贴近真实时的粒度 | 当前 rjw-cummilk | 说明 |
|---------------|-------------------|------------------|------|
| 腺泡内乳量（涨奶） | 每侧乳腺实质 | `poolKey_L` / `poolKey_R` 独立 fullness、容量、压力因子、回缩 | 与「每乳分左右」一致 |
| 泌乳驱动（催乳素轴、L） | 全身性 + 排空反馈 | `CurrentLactationAmount`、`RemainingDays`、`drive` 等在 **Lactating Hediff** 上全局；排空/挤奶刺激 L 亦全局 | 与「泌乳时间等综合量全局」一致 |
| 合成—分泌速率 | 受局部充盈度反馈 | 全局 `basePerDay` × 每侧 `conditions×pressure×letdown×FlowMultiplier×density` | 侧别反馈已拆；`basePerDay` 不拆 |
| 喷乳反射（催产素介导） | 常可由单侧吸吮/刺激触发 | `letdownReflexByKey` **按 sideKey**；挤奶扣池按 drainedKeys 逐侧 `AddLetdownReflexStimulus` | 与单侧刺激一致 |
| 炎症负荷（淤积—感染前状态） | 每侧可独立 | `inflammationByKey` 按池条目 key；`UpdateInflammation` 逐 entry | 与单侧一致 |
| 乳腺炎/脓肿/瘀积 **Hediff** | 临床可单侧 | 多为 **一条** Def 挂在胸部/乳房 `BodyPart`；`GetConditionsForSide` 用「该对」`Part`，故 **同一 Part 上左右虚拟池共受同一乳腺炎 severity** | RimWorld / RJW 部位粒度限制；非 mod 池模型问题 |
| I 超阈触发乳腺炎 | 可单侧先发病 | `TryTriggerMastitisFromInflammation` 用 `GetInflammationMax()`（**任一侧** max）判定，发病后 **全体侧** I 一起减轻 | 简化：一次事件处理「全身发作」 |
| 乳房胀满 `EM_BreastsEngorged` | 可单侧 | **任一侧** `fullness ≥ 满阈×该侧基础容量` 则加 Hediff；**每一侧**都低于「0.9×该侧基础容量」才移除 | Hediff 仍一条挂胸；**判定按虚拟侧** |
| 奶水瘀积 `EM_LactationalMilkStasis` | 可单侧 | 久满/胀满+时间用 **各侧** `ticksFullPoolByKey` 的 **最大值**；缓解要求 **每一侧** 相对撑大比例均低于 0.56 | 与总奶量脱钩 |
| MTB 乳腺炎 / 化脓进展 | 可单侧 | **每侧**独立累计 `ticksFullPoolByKey`；`TryTriggerMastitisFromMtb` 对 **每个已满 ≥1 天的一侧** 单独 `MTBEventOccurs`；仅卫生/伤、无一侧久满时 **全局一次** MTB | 发病后仍为单条 `EM_Mastitis`（部位粒度限制） |
| 满池提醒信件 | 可单侧触发 | `GetMaxTicksFullPoolAcrossSides` ≥1 天即可能发信 | 与「任一侧久满」一致 |
| 溢出时残余流速 | 可与局部相关 | `ApplyOverflowResidualFlow` 内 `resL` 用 **全局** `CurrentLactationAmount` | 简化：泌乳强度不按侧拆 |
| L 每日衰减中的炎症项 | 全身应激 | `GetDailyLactationDecayWithBT` 用 `GetInflammationMax()` | 最差一侧炎症拉高整体衰减，偏保守 |
| RJW 单条乳房 Hediff 对 `_L/_R` | 真实可不对称 | 同一对 **两侧共用** `BaseCapacityPerSide`、`FlowMultiplier`（来自同一 Hediff） | 不对称体积/流速需 RJW 提供侧别数据或额外规则 |

**汇总 API 注意**：`GetBreastCapacityFactors`（经 `GetLeftBreastCapacityFactor` + `GetRightBreastCapacityFactor`）把 **所有乳房对** 的单侧基容量之和 **左右各写一份相同值**，使 `左+右` 等于全部虚拟池基容量之和，**不是**「只有左胸那些乳房」的解剖语义；**逐对** 乳房行 Tooltip、流速请以 `GetBreastPoolEntries` / `GetFlowPerDayForBreastPair` / `poolKey_L`·`poolKey_R` 为准。

### 阶段 5：建议手测场景（非自动化）

人类单对乳、多乳房种族（多条 Hediff 时每条仍拆 _L/_R）、无乳房、永久泌乳、催乳药、**炎症/胀满/瘀积/满池 tick/MTB 均按虚拟侧或取侧上 max**、容量模式切换严重度/重量/体积。

| 设置项 | 类型 | 说明 | 影响的 RJW 相关逻辑 |
|--------|------|------|----------------------|
| **rjwBreastSizeEnabled** | bool | 是否启用「按 RJW 乳房体型参与乳池/容量/流速」 | 关闭后 GetBreastCapacityFactors、GetBreastPoolEntries 等直接 return 或返回空；RJWLactatingBreastSize 的 Severity 增益也不执行 |
| **rjwBreastPoolCapacityMode** | enum | **三档**：`Severity` / `RjwBreastWeight` / `RjwBreastVolume`（新档默认体积） | `RjwBreastPoolEconomy.ComputeBaseCapacityPerSide`；读档时非法枚举值会 Clamp 到 0–2 |
| **rjwBreastCapacityCoefficient** | float | 乳池容量系数，默认 2 | 单侧基容量 = `Clamp(基值 * 本系数, 0, 10)` |
| **rjwNippleStageFlowBonusPercent** | float | 阶段标签含 “Nipple” 时流速倍率 ±%（0=关） | `GetBreastHediffFlowMultiplier` |

---

## 三、rjw-cummilk 中涉及 RJW 乳房的主要代码位置

| 文件 | 用途简述 |
|------|----------|
| `RJWVersionDiffHelper.cs` | 封装 `GetBreasts()`，兼容 RJW 1.5/1.6 |
| `RjwBreastPoolEconomy.cs` | `GetBreastPairSnapshots` / `BuildPoolKey` / `RjwBreastPairSnapshot`：与池条目、高潮进池、泌乳期 Severity 同步共用同一套键与经济数据 |
| `PawnMilkPoolExtensions.cs` | 用 `GetBreastList()`、上述经济层与 `Severity` 等计算虚拟左右池容量、池条目、池 key、身体部位 |
| `RJW.cs` | Harmony Patch：MilkAmount 按乳房 FluidMultiplier 乘算；MilkDef 按 Fluid.consumable 取奶制品 Def |
| `RJWLactatingBreastSize.cs` | 泌乳期按池/L 同步 `GetBreastList` 中进池乳房的 Severity；离乳恢复。存档键 `EM.RjwBreastBaseSeverity`（键值对列表），不再使用旧的 Pending 列表 |
| `MilkingPatch.cs` (HediffComp_SexPart_CompTipStringExtra_Patch) | 仅当 `genitalFamily == Breasts` 时在健康页乳房行 Tooltip 后追加奶池块 |
| `MilkCumSettings.cs` | `rjwBreastSizeEnabled`、`rjwBreastPoolCapacityMode`、`rjwBreastCapacityCoefficient` 等；Mod 设置存档键 `EM.RjwBreastPoolCapMode`（旧 `EM.RjwBreastPoolCapacityMode` 不再读取） |
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
本 mod 只使用 RJW 的**结果**：`Hediff.Severity`、`def.fluidMultiplier`、`sizeProfile.density`；并仅在容量模式为「重量」时读取一次 `PartSizeCalculator.TryGetBreastSize(...).weight`，避免重复计算与逻辑不一致。

---

## 七、未使用的 RJW 乳房相关项（摘要）

- **PartSizeConfigDef**：cupSizes、bodysizescale 未使用；**density 已接入**（见上）。  
- **BraSizeConfigDef / BreastSize**：罩杯字母、底围、体积在本 mod 未直接使用；`weight` 仅在容量模式为「重量」时间接读取。  
- **PartSizeCalculator**：`TryGetCupSize` 等未调用；`TryGetBreastSize` 在 `RjwBreastPoolEconomy.ComputeBaseCapacityPerSide` 中用于读取 `weight`。  
- **HediffDef_SexPart**：fluid、stages、defaultBodyPart、sizeMultiplier 未直接参与本 mod 的乳池或 UI 逻辑（fluid 通过 Comp 的 Fluid 间接用于奶制品类型；sizeProfile.density 已用于奶量增加倍率）。

---

*文档版本：基于 RJW 1.6 与 rjw-cummilk 当前源码整理。*  
*最后与代码核对：2026-03-24*
