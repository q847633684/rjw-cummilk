# RJW（1.6）乳房相关定义 — 字段总表

本文档按 **RJW 源码与 Def** 归纳「乳房」在框架内的**全部可用定义**；解剖上的乳头/乳晕在 RJW 中**无独立数值字段**（仅有阶段文案等）。路径以本地 mod 为准：`rjw/1.6/Source`、`rjw/1.6/Defs`。

---

## 一、`HediffDef_SexPart`（乳房 Hediff 的 Def 本体）

| 字段 | 类型 | 乳房场景含义 |
|------|------|----------------|
| `fluid` | `SexFluidDef` | 分泌流体类型；乳房模板一般为 **Milk**。 |
| `fluidMultiplier` | `float`（默认 1） | Def 级流体量/泌乳相关**基础倍率**；最终会与严重度、体型、年龄、`HediffComp_SexPart.partFluidMultiplier` 等组合（见 `GetFluidMultiplier`）。 |
| `produceFluidOnOrgasm` | `bool` | 高潮时是否按该 Def **额外产液**；具体是否开启以各 Def/XML 为准。 |
| `defaultBodyPart` | `string` | 默认身体部位关键字；乳房抽象为 **Chest**。 |
| `defaultBodyPartList` | `List<string>` | 可安装的默认部位列表（与手术/纠错有关）。 |
| `sizeMultiplier` | `float`（默认 1） | `GetRandomSize` 等随机尺寸的额外倍率。 |
| `sizeProfile` | `PartSizeConfigDef` | **必须**（非 `Undefined` 族时 `ConfigErrors` 会报错）：指向如 `DefaultBreastSizes`，提供 **cup 曲线、density、bodysizescale**。 |
| `partTags` | `List<string>` | 通用零件标签（乳房可在具体 Def 中扩展）。 |
| `genitalFamily` | `GenitalFamily` | 乳房为 **`Breasts`**。 |
| `genitalTags` | `List<GenitalTag>` | 能力标签；乳房抽象含 **`CanLactate`** 等。 |
| `stages` | `List<HediffStage>` | **整胸**大小阶段（`minSeverity`、`label`、`capMods`）；**非**乳头/乳晕分零件。首档常 **Nipples**（极小/男性胸阶段文案）。 |
| （继承 `HediffDef`） | — | `defName`、`label`、`description`、`hediffClass`（如 `Hediff_NaturalSexPart`）、comps 列表等。 |

**源码**：`rjw/1.6/Source/Hediffs/HediffDef_SexPart.cs`  
**抽象乳房模板**：`rjw/1.6/Defs/HediffDefs/Hediffs_PrivateParts/Hediffs_PrivateParts.xml` → `NaturalPrivatePartBreast`  
**人类实例**：同目录 `Hediffs_PrivateParts_Human.xml` → `Breasts`；平胸变种 → `FeaturelessChest`（不继承乳房抽象，无 `sizeProfile` 等乳房经济）

---

## 二、`PartSizeConfigDef`（乳房尺寸配置，被 `sizeProfile` 引用）

| 字段 | 类型 | 乳房场景含义 |
|------|------|----------------|
| `defName` / `label` | Def 标识 | 如 `DefaultBreastSizes`。 |
| `bodysizescale` | `bool` | 是否按**初始种族体型**缩放尺寸相关曲线。 |
| `density` | `float?` | **组织密度/比重**：人类常 **1.0**；`null` 时部分重量显示与按重反算 Severit** y **不可用**。也参与 **`BreastSize.weight = volume × density`**。 |
| `cupSizes` | `List<float>` | 与 **`HediffDef.stages` 顺序对应** 的罩杯**刻度索引**（可负数）；用于 `PartSizeCalculator` 插值 Cup。 |
| `lengths` | `List<float>` | 乳房 **通常不用**；阴茎等用的长度曲线。 |
| `girths` | `List<float>` | 乳房 **通常不用**；阴茎等用的周长曲线。 |

**源码**：`rjw/1.6/Source/Hediffs/PartSizeConfigDef.cs`  
**Def**：`rjw/1.6/Defs/ConfigDefs/PartSizeConfigs.xml` → `DefaultBreastSizes`

---

## 三、`HediffComp_SexPart`（实例运行时，挂在乳房 Hediff 上）

| 字段/成员 | 类型 | 乳房场景含义 |
|-----------|------|----------------|
| `baseSize` | `float` | 当前胸部**绝对尺寸**基底（可与成长 Comp 等联动）。 |
| `forcedSize` | `float?` | 有值时**覆盖** `GetSize()`，用于固定展示/调试等。 |
| `originalOwnerRace` / `previousOwner` / `isTransplant` | 字符串/布尔 | 移植、来源种族等元数据。 |
| `partFluidMultiplier` | `float`（默认 -1 表示未设） | **永久**流体量倍率，参与 `Def.GetFluidMultiplier(...)`。 |
| `fluidOverride` | `SexFluidDef` | 覆盖 Def 的 `fluid`。 |
| `GetSize()` | 方法 | 返回 `forcedSize ?? baseSize`。 |
| `GetSeverity()` | 方法 | `GetSize() / Pawn.BodySize`，与 `Hediff.Severity` 同步逻辑一致。 |
| `SetSeverity` / `SyncSeverity` | 方法 | 用严重度反推 `baseSize` 并写回 `parent.Severity`。 |
| `GetFluidMultiplier()` / `GetFluidAmount()` | 方法 | RJW 原版泌乳/射出量计算入口（与 **Severity、体型、年龄** 挂钩）。 |

**源码**：`rjw/1.6/Source/Hediffs/Comps/HediffComp_SexPart.cs`

---

## 四、`BraSizeConfigDef` + `BreastSize`（杯/带/体积/重量）

| 名称 | 类型 | 含义 |
|------|------|------|
| `BraSizeConfigDef.bandSizeBase` | `float` | 人类标准底围基准；与小人 `BodySize` 组合得到 **`bandSize`**（取偶）。 |
| `BraSizeConfigDef.cupSizeLabels` | `List<string>` | 罩杯**字母/档位**显示用下标表。 |
| `BreastSize.bandSize` | `float` | 底围（数值尺度）。 |
| `BreastSize.cupSize` | `float` | 罩杯刻度（连续值，来自曲线）。 |
| `BreastSize.density` | `float` | 来自 `PartSizeConfigDef.density`（缺省在部分路径按 1 处理）。 |
| `BreastSize.volume` | 属性（升） | `CalculateVolume(cupSize, bandSize)`，经验公式。 |
| `BreastSize.weight` | 属性（千克） | `volume × density`。 |
| 静态方法 | — | `CalculateCupSize` / `CalculateBandSize` 等用于反算。 |

**源码**：`rjw/1.6/Source/Common/Data/BraSizeConfigDef.cs`  
**Def**：通常是 `BraSizeConfigDef` 的单例 xml（与 RJW Defs 中 Cup 标签表对应，具体文件名以工程为准）
RjwBreastVolume
---

## 五、`PartSizeCalculator`（乳房相关 API 摘要）

| API | 作用 |
|-----|------|
| `TryGetBreastSize(Hediff, out BreastSize)` | 综合 **Cup 曲线 + Band + density** 得到 `BreastSize`。 |
| `TryGetCupSize` / `TryGetBandSize` | 分步取杯刻度、底围。 |
| `Curves.CupSizeCurve(def)` | `stages` 与 `sizeProfile.cupSizes` **拉链** 成 `SimpleCurve`。 |
| `Inversion.TryInvertBreastWeight` / `TryInvertBreastVolume` | 由重量或体积反推 Severit** y **等（需 `density` 非 null）。 |
| `Modify.TryModifyBreastVolume` / `TryModifyBreastWeight` | 改体积/重量目标后反算新严重度。 |

**说明**：`TryGetLength` / `TryGetGirth` / `TryGetPenisWeight` 对**乳房 Def** 通常无有效曲线（`DefaultBreastSizes` 未配 lengths/girths）。

**源码**：`rjw/1.6/Source/Hediffs/PartSizeCalculator.cs`

---

## 六、`HediffDef_SexPart.GetFluidMultiplier` 输入因子（与「泌乳强度」相关）

| 因子 | 来源 | 作用（简述） |
|------|------|----------------|
| `fluidMultiplier`（Def） | XML/C# | 基础倍率。 |
| `pawnAge` | 参数 | 约 **50 岁以上** 按代码压低贡献。 |
| `pawnBodySize` | 参数 | 大体型增大乘数（与 Severity 已含体型的设计搭配使用，见源码注释）。 |
| `partSeverity` | `GetSeverity()` | 高于平均放大、低于平均缩小（分段公式）。 |
| `partFluidMultipler` | Comp | 实例永久倍率。 |

**源码**：`HediffDef_SexPart.GetFluidMultiplier`（同 `HediffDef_SexPart.cs`）

---

## 七、RJW **未**建模项（避免对外文档误读）

| 概念 | RJW 内状态 |
|------|-------------|
| 乳头（独立部位/参数） | **无**独立 Def/曲线；仅阶段名 **Nipples** 等文案。 |
| 乳晕（直径/颜色/独立 Hediff） | **无**对应字段。 |
| 左右乳分别 Severit** y ** | **一条 Hediff** 表示**一对乳房**，不拆分 L/R（L/R 虚拟侧为 **rjw-cummilk** 等下游扩展）。 |

---

## 八、与 rjw-cummilk 的交叉引用（非 RJW 本体，仅供对照）

| rjw-cummilk 概念 | 对应 RJW 来源 |
|------------------|----------------|
| 进池 Hediff | `HediffDef_SexPart.genitalTags` 含 **`GenitalTag.CanLactate`**（与 RJW 泌乳标签一致；无则不进虚拟池） |
| 容量公式中的「严重度」 | 优先 **`HediffComp_SexPart.GetSeverity()`**，否则回退 `Hediff.Severity`；**仅**设置选「纯严重度」档时使用 |
| 容量三档（`RjwBreastPoolCapacityMode`） | **`Severity`** / **`RjwBreastWeight`** / **`RjwBreastVolume`**（新档默认体积）；重量/体积档无有效 RJW 尺寸则为 0（不进池） |
| `FlowMultiplier`（每对） | **`HediffComp_SexPart.GetFluidMultiplier()`**（与 RJW 相同、**无**额外 0.1–3 上限）× 乳头阶段装饰；无 Comp 时回退 `Def.GetFluidMultiplier(...)` |
| `PartSizeConfigDef.density` | **RJW 内**用于 `weight = volume × density` 等；**rjw-cummilk 不在进水/高潮灌池上乘密度**（与 `GetFluidMultiplier` 分工一致） |
| 虚拟池 `poolKey_L/R` | 基于 `GetBreastList` 每条乳房 Hediff **拆两侧**，非 RJW 原版字段 |

---

*表格式整理自当前工作区 `rjw/1.6` 源码与 Def；若 RJW 更新 Def 结构，以官方仓库为准。*
