# RJW 数据对齐清单与第三方集成

本文档约定：**哪些逻辑应以 RJW 运行时数据为输入（只读或受控写回）**，**哪些属于本 mod 水池模型自有**，以及**其它模组如何安全接入**。与 [RJW乳房定义与rjw-cummilk使用说明.md](RJW乳房定义与rjw-cummilk使用说明.md) 互补：前者偏 Def/API 字段映射，本文偏架构边界与集成策略。

**最后整理**：2026-04-09

---

## 一、总原则

1. **解剖与「谁算乳房、能否泌乳」**：以 RJW 的乳房列表、`GenitalTag.CanLactate`、`HediffDef_SexPart` 为唯一事实来源；本 mod 不维护平行的「胸数量/叶数」规则。
2. **乳池容量的物理输入**：单侧基容量由设置 `rjwBreastPoolCapacityMode` 在 **严重度 / RJW 体积 / RJW 重量** 三选一，数据来自 `GetSeverityForPoolEconomy`、`PartSizeCalculator.TryGetBreastSize`；与 RJW 基因、阶段、手术对 Severity 或尺寸的修改对齐。
3. **乳池流速（进水侧倍率）**：单侧 `GetBreastHediffFlowMultiplier` → 优先 `HediffComp_SexPart.GetFluidMultiplier()`，与 RJW（及药品 `partFluidMultiplier` 等）一致；**不等于**容量公式，倍率大不自动等于「池更大」。
4. **水池积分、满池、回缩、营养、炎症、导管、拟真子系统**：本 mod 规格内自洽；RJW 只提供上两条的**输入**，不参与逐 tick 池内公式的重复实现。
5. **精液**：独立 `RjwSemenPoolEconomy`，只认 RJW **可射精部件**的 `GetFluidAmount` / `GetFluidMultiplier`；与泌乳 `CompMilkable.fullness`、泌乳 `Charge` 桥接无关。
6. **Legacy 兼容**：`fullness` / `Charge` 桥接与设置 `bridgeExternalCompMilkableFullness`、`bridgeExternalLactatingCharge` 兜第三方**直接写字段**；**不推荐**作为新模组首选，首选程序集 API（见第四节）。

---

## 二、建议与 RJW 对齐的内容（维护时检查清单）

| 主题 | 应对齐的 RJW 数据 | 本 mod 主要入口 | 说明 |
|------|-------------------|-----------------|------|
| 乳房是否进池 | `GenitalTag.CanLactate`、乳房 Hediff 列表 | `RjwBreastPoolEconomy.IsBreastHediffForPool` | 与 RJW 泌乳标签一致 |
| 池 key / 拓扑 | `Hediff.Part` 路径、`MakeStablePoolKey` | `RjwBreastPoolEconomy`、`PawnMilkPoolExtensions` | 多叶、手术改部位时键迁移见 PoolStorage |
| 容量（单侧基容） | Severity 或 BreastSize volume/weight | `ComputeBaseCapacityPerSide`、`MilkCumSettings.rjwBreastPoolCapacityMode` | 三档与基因/药「改的是哪一维」需一致（见第五节） |
| 流速倍率 | `GetFluidMultiplier` | `GetBreastHediffFlowMultiplier` | 药品/基因改倍率会体现为进水快慢 |
| 泌乳期胸型表现 | `Hediff` Severity | `RJWLactatingBreastSizeGameComponent` | 池撑张 ↔ Severity；勿另造一套永久 Severity 规则 |
| 高潮产奶 | `produceFluidOnOrgasm` | RJW 集成 Patch（Orgasm 后灌池） | 子 mod 开此 Def 字段即联动 |
| 性/吸奶钩子 | RJW 行为与 Need | `RJWLustIntegration`、`BreastfeedPatch` 等 | 增量逻辑，不复制 RJW 整段判定 |
| 精液容量与流速 | 阴茎类 `GetFluidAmount`、`GetFluidMultiplier` | `RjwSemenPoolEconomy` | 与乳房逻辑分离 |

---

## 三、明确保持本 mod 自有的内容（不必与 RJW 数值强行一致）

| 主题 | 说明 |
|------|------|
| 池单位、1 池 = 1 瓶、挤奶工作时长 | 游戏性规格，见 [泌乳系统逻辑图.md](泌乳系统逻辑图.md) |
| 权限表、默认吸奶/挤奶名单、牛奶主表 UI | 可默认引用关系，规则以本 mod 设置为准 |
| 组织适应、炎症四层模型、导管、满池污物与信件 | 本 mod 模型；读 RJW 部位与倍率，不反向要求 RJW 理解这些字段 |
| `ApplyExternalTotalTarget` 触发的副作用 | 排空炎症缓解、`lastGatheredTick`、进水突发、`SyncChargeFromPool` / `OnGathered` 等 |

---

## 四、第三方模组集成（推荐顺序）

1. **首选**：程序集 API `MilkCum.Integration.EqualMilkLactationApi`（`TryApplyTotalPoolTarget`、`TryDrainPoolForConsume`、`SyncLactatingChargeFromPool`）。单位与 UI/Charge 一致，为**池单位总量**，不是 0～1 归一化百分比。
2. **按 key 加减**：`CompEquallyMilkable.AddMilkToKeys`（高潮产液、RJW 路径已用）。
3. **Legacy**：仅写 `CompMilkable.fullness` 或 `HediffComp_Lactating.Charge` 时，依赖游戏内桥接开关；可能与其它路径**双重结算**，见设置说明与 Dev 日志 `logExternalFullnessBridge`。
4. **勿假设**：本 mod 会猜对方 mod 的「业务语义」（权限、心情、是否算一次挤奶）；桥接只做**总量对齐**与已接好的游戏副作用。

---

## 五、基因 / 药物与 `rjwBreastPoolCapacityMode` 对照（避免「看起来不兼容」）

| 对方更可能改动的量 | 建议容量模式 | 现象若不一致时 |
|--------------------|--------------|----------------|
| 主要抬 **Severity**、乳房阶段 | **严重度** 档 | 体积档下池上限可能几乎不变，但流速仍可能因倍率变快 |
| 主要抬 **BreastSize 体积/重量**（RJW 计算器有效） | **体积** 或 **重量** 档 | 严重度档下上限可能主要跟 Severity 走 |
| rjw-genes 等未走标准 API | 无通用保证 | 需对方改到 RJW 认的 Hediff/Severity/尺寸/倍率，或专用补丁 |

---

## 六、相关文档

- [RJW乳房定义与rjw-cummilk使用说明.md](RJW乳房定义与rjw-cummilk使用说明.md) — Def 字段与代码映射  
- [游戏已接管变量与机制清单.md](游戏已接管变量与机制清单.md) — 原版/RJW 已接管、本 mod 只读  
- [Integrations索引.md](Integrations索引.md) — RJW 代码目录与 Harmony  
- [参数联动表.md](参数联动表.md) — `rjwBreastCapacityCoefficient`、`rjwBreastPoolCapacityMode`、桥接开关等  
- [泌乳系统逻辑图.md](泌乳系统逻辑图.md) — 水池权威流程  

---

## 七、代码锚点（便于检索）

- 乳池经济：`Source/MilkCum/Fluids/Lactation/Helpers/RjwBreastPoolEconomy.cs`  
- 精液经济：`Source/MilkCum/Fluids/Cum/Common/RjwSemenPoolEconomy.cs`  
- 泌乳胸型同步：`Source/MilkCum/Integration/RJW/RJWLactatingBreastSize.cs`  
- 第三方 API：`Source/MilkCum/Integration/EqualMilkLactationApi.cs`  
- 外部镜像桥接：`CompEquallyMilkable.TryBridgeExternalFullnessWrite` / `TryBridgeExternalChargeWrite`；Charge 与 Hediff tick 协同：`HediffComp_EqualMilkingLactating.SyncChargeWithPoolForExternalBridge`  

---

## 八、追加的 RJW 对齐（补充清单）

以下条目与第二节表**互补**：偏 **挤奶 Comp 替换、奶类型、性/生育钩子、无 RJW 降级**；实现分散在 RJW 集成与 Harmony，维护时一并核对。

| 主题 | 应对齐的数据或机制 | 本 mod 主要入口 | 说明 |
|------|-------------------|-----------------|------|
| **奶物品 / MilkDef** | RJW 性器 Fluid、种族奶类型 | `Source/MilkCum/Integration/RJW/RJW.cs`，`CompEquallyMilkable.ResourceDef` → `Pawn.MilkDef()` | 产出物 Def 随 RJW/种族解析，不手写死单一 ThingDef |
| **CompMilkable → 平等挤奶** | 原版 `CompProperties_Milkable.compClass` | `Source/MilkCum/Harmony/Lactation/MilkingPatch.CompSwap.cs` → `CompProperties_Milkable_Patch`（构造 Postfix） | 小人挤奶仍走 gatherable body resource，逻辑在 `CompEquallyMilkable` |
| **基类 Active 与多态** | `CompMilkable.Active` 基实现 | 同文件 `CompMilkable_Patch`（Active Getter Prefix） | **实例上**以 `CompEquallyMilkable` 的 `Active` override 为准；仅当代码**强制按基类型**调 `CompMilkable.Active` 时才命中 Patch |
| **性交 / 高潮产液** | `JobDriver_Sex`、Orgasm | `RJW.cs` 等 Harmony（如 Orgasm 后灌池） | 与 `produceFluidOnOrgasm`、设置 `rjwSexAddsLactationBoost` 等联动 |
| **泌乳期生育力** | RJW 怀孕相关 | `RJWSexAndFertility.cs` | `rjwLactationFertilityFactor` 等；不重复实现完整妊娠树 |
| **欲望 / 吸奶类工作** | RJW Job、Need | `RJWLustIntegration.cs` | 在 RJW 工作链上增量挂接 |
| **流体倍率警报** | 健康/流体展示 | `Alert_FluidMultiplier.cs` | 与 RJW 倍率展示一致化（若版本差异见 `RJWVersionDiffHelper`） |
| **无 RJW mod** | — | `ModIntegrationGates.RjwModActive` | `false` 时乳房池不读 RJW 列表/体积；走对称双池等非 RJW 路径（见 `PawnMilkPoolExtensions`） |
| **药品 / 泌乳 Hediff 日衰减** | 原版 `SeverityPerDay`、化学耐受 | 见 [游戏已接管变量与机制清单.md](游戏已接管变量与机制清单.md) | 本 mod **不重写**耐受与日衰减公式；水池 L 与 Severity 同步策略见 [泌乳系统逻辑图.md](泌乳系统逻辑图.md) |
| **乳腺炎卫生** | （非 RJW）DBH Hygiene | `Source/MilkCum/Integration/DubsBadHygiene/DubsBadHygieneIntegration.cs` | 与 RJW 并行：卫生系数进炎症模型，不冒充 RJW API |

**维护提示**：新增任何「读乳房/读精液/改 Severity」的代码前，先确认是否已有 `RjwBreastPoolEconomy` / `RjwSemenPoolEconomy` / `RJWLactatingBreastSizeGameComponent` 路径可复用，避免第三条原则被违反（重复实现 RJW 已维护的量）。
