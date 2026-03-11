# Source 目录结构梳理

以 **Source/索引.md** 为基准，对项目目录层级与代码结构做系统对照后的结论与约定。  
**框架原则**：本 mod 为**流体系统**，包含**精液、乳汁**（母乳/奶）、**妹汁**（阴道分泌液）三大系统；**一个系统对应一个目录**（见 [domain/mod定位-流体系统三大系统](../domain/mod定位-流体系统三大系统.md)）。  
**当前实现**：Fluids 下 Lactation（乳汁）、Cum（精液）、Shared（共享）；Core/、Integration/、Harmony/、UI/、Debug/ 与设计一致。

---

## 一、三大系统与目录对应

| 系统 | 目录 | 命名空间/说明 |
|------|------|----------------|
| **乳汁**（母乳/奶） | `MilkCum/Fluids/Lactation/` | 泌乳、双池、挤奶/吸奶、耐受、喷乳反射 |
| **精液** | `MilkCum/Fluids/Cum/` | Cumpilation.*（Cumflation、Gathering、Leaking、Bukkake 等） |
| **妹汁**（阴道分泌液） | 待定 | 第三大系统，若在本仓库则于 Fluids 下新增对应目录 |
| **共享** | `MilkCum/Fluids/Shared/` | 池模型、FluidPoolState/Entry/FluidTag、营养/能量 Helper（非独立系统） |

乳汁（母乳）内部可再分为三个逻辑子系统：**泌乳系统**（L、衰减、hediff）、**池系统**（双池、进水、容量）、**挤奶/吸奶系统**（取奶、Job、WorkGiver）；目录是否按此拆分子目录见 [乳汁系统内子系统划分与目录建议](乳汁系统内子系统划分与目录建议.md)。

---

## 二、当前目录层级（Fluids 结构）

| 路径 | 职责 | 命名空间 |
|------|------|----------|
| `MilkCum/Core/` | 模组入口、DefOf、建筑、StatWorker 安全封装 | `MilkCum.Core` |
| `MilkCum/Core/Settings/` | 全局设置（EqualMilkingSettings） | `MilkCum.Core.Settings` |
| `MilkCum/Core/Utils/` | EventHelper、Lang、VersionDiffHelper | `MilkCum.Core.Utils` |
| `MilkCum/Core/Constants/` | Constants、PoolModelConstants | `MilkCum.Core.Constants` |
| `MilkCum/Core/Stats/` | StatWorker_PawnOnlySafe | `MilkCum.Core.Stats` |
| **Fluids/** | **统一体液系统** | |
| `MilkCum/Fluids/Lactation/Comps/` | 泌乳 Comp、CompProperties（池、精液桶等） | `MilkCum.Fluids.Lactation.Comps` |
| `MilkCum/Fluids/Lactation/Hediffs/` | Hediff*（泌乳、吸收延迟、耐受、增益） | `MilkCum.Fluids.Lactation.Hediffs` |
| `MilkCum/Fluids/Lactation/Data/` | 乳汁数据（RaceMilkType、MilkTag、MilkSettings、Breastfeed 等） | `MilkCum.Fluids.Lactation.Data` |
| `MilkCum/Fluids/Lactation/Helpers/` | 扩展、工具（池、权限、健康、Childcare 等；FoodEnergy/Texture 已迁 Shared） | `MilkCum.Fluids.Lactation.Helpers` |
| `MilkCum/Fluids/Lactation/Jobs/` | JobDriver | `MilkCum.Fluids.Lactation.Jobs` |
| `MilkCum/Fluids/Lactation/Givers/` | WorkGiver / JobGiver | `MilkCum.Fluids.Lactation.Givers` |
| `MilkCum/Fluids/Lactation/Thoughts/` | ThoughtWorker | `MilkCum.Fluids.Lactation.Thoughts` |
| `MilkCum/Fluids/Lactation/World/` | WorldComponent（吸收延迟等） | `MilkCum.Fluids.Lactation.World` |
| `MilkCum/Fluids/Shared/Data/` | 共享池与标签（FluidPoolState、FluidPoolEntry、FluidTag） | `MilkCum.Fluids.Shared.Data` |
| `MilkCum/Fluids/Shared/Models/` | 流速/压力/营养模型占位（FlowModel、PressureModel、NutritionModel） | `MilkCum.Fluids.Shared.Models` |
| `MilkCum/Fluids/Shared/Helpers/` | FoodEnergyHelper、TextureHelper | `MilkCum.Fluids.Shared.Helpers` |
| `MilkCum/Fluids/Cum/` | 精液子模块（Bukkake、Cumflation、Gathering、Leaking 等） | `Cumpilation.*` |
| `MilkCum/Harmony/Lactation/` | 乳汁相关补丁（MilkingPatch、BreastfeedPatch、MilkProductConsumptionPatch） | `MilkCum.Harmony` |
| `MilkCum/Harmony/` | 其余补丁（Compatibility、Consumption/Recipe 等） | `MilkCum.Harmony` |
| `MilkCum/Integration/RJW/` | RJW 集成 | `MilkCum.RJW` 等 |
| `MilkCum/Integration/VanillaExpanded/` | VCE（Vanilla Milk Expanded） | `MilkCum.Integration.VanillaExpanded` |
| `MilkCum/Integration/PipeSystem/` | 奶管道（VE Framework） | `MilkCum.PipeSystem` |
| `MilkCum/Integration/DubsBadHygiene/` | Dubs Bad Hygiene | `MilkCum.Integration.DubsBadHygiene` |
| `MilkCum/UI/` | 设置界面、表格、列、窗口 | `MilkCum.UI` |
| `MilkCum/UI/Tables/` | PawnTable_Main、PawnColumnWorker_* | `MilkCum.UI` |
| `MilkCum/Data/Records/` | MilkRecord 等 DTO | `MilkCum.Data.Records` |
| `MilkCum/Data/Models/` | MilkQualityModel 等 | `MilkCum.Data.Models` |
| `MilkCum/Debug/` | 调试工具（DevTools 等） | `MilkCum.Debug` |

**说明**：模组入口为 **ModInit**（`Core/ModInit.cs`）。共享池类型为 **FluidPoolState**、**FluidPoolEntry**（`Fluids/Shared/Data/`）；精液子模块保留命名空间 **Cumpilation.***，目录为 **Fluids/Cum/**。Core/Settings 含 EqualMilkingDefaultsDef；Core/Stats 含 StatWorker_PawnOnlySafe。

---

## 三、目标结构（用户给定）与当前实现对照

| 目标 | 当前实现 |
|------|----------|
| Core/ | ✅ Core/（ModInit、Settings、Utils、Constants） |
| Fluids/Lactation/（Comps, Hediffs, Jobs, Data） | ✅ Fluids/Lactation/（含 Helpers、Givers、Thoughts、World） |
| Fluids/Cum/（Comps, Hediffs, Cumflation, Bukkake） | ✅ Fluids/Cum/（Cumpilation.* 子模块） |
| Fluids/Shared/（Data: FluidPoolState/Entry/FluidTag；Models；Helpers） | ✅ Fluids/Shared/Data、Models、Helpers |
| Integration/（RJW, DubsBadHygiene, VanillaExpanded） | ✅ Integration/（RJW、DubsBadHygiene、VanillaExpanded/VCE、PipeSystem） |
| Harmony/、UI/、Debug/ | ✅ 与设计一致 |

---

## 四、后续新增文件约定

1. **放哪**：按职责放入上表对应目录；**新系统**在 `MilkCum/Fluids/` 下新建与「乳汁」「精液」平级的目录（一系统一目录）；新集成 mod 放在 `MilkCum/Integration/` 下新建子文件夹。
2. **索引**：在 **Source/索引.md** 对应章节表格中新增一行，填写文件名、类型、主要函数/成员、功能简述。
3. **命名空间**：新文件命名空间与所在目录一致；GlobalUsings 已包含 Core.Settings、Core.Stats、Core.Utils、Core.Constants、Fluids.Lactation.*、Fluids.Shared.Data/Models/Helpers、Harmony、Integration.DubsBadHygiene。

---

## 五、相关文档

- 目录与类型明细：**Source/索引.md**
- Mod 定位与三大系统：**记忆库/domain/mod定位-流体系统三大系统.md**
- 集成模块一览：**记忆库/docs/Integrations索引.md**
- 架构原则与重组建议：**记忆库/design/架构原则与重组建议.md**
