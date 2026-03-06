# Source 目录结构梳理

以 **Source/索引.md** 为基准，对项目目录层级与代码结构做系统对照后的结论与约定。  
**当前采用**：统一体液系统 **Fluids/**（Lactation 乳汁 + Cum 精液 + Shared 共享模型），Core/、Integration/、Harmony/、UI/、Debug/ 与设计一致。

---

## 一、当前目录层级（Fluids 结构）

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

## 二、目标结构（用户给定）与当前实现对照

| 目标 | 当前实现 |
|------|----------|
| Core/ | ✅ Core/（ModInit、Settings、Utils、Constants） |
| Fluids/Lactation/（Comps, Hediffs, Jobs, Data） | ✅ Fluids/Lactation/（含 Helpers、Givers、Thoughts、World） |
| Fluids/Cum/（Comps, Hediffs, Cumflation, Bukkake） | ✅ Fluids/Cum/（Cumpilation.* 子模块） |
| Fluids/Shared/（Data: FluidPoolState/Entry/FluidTag；Models；Helpers） | ✅ Fluids/Shared/Data、Models、Helpers |
| Integration/（RJW, DubsBadHygiene, VanillaExpanded） | ✅ Integration/（RJW、DubsBadHygiene、VanillaExpanded/VCE、PipeSystem） |
| Harmony/、UI/、Debug/ | ✅ 与设计一致 |

---

## 三、后续新增文件约定

1. **放哪**：按职责放入上表对应目录；新集成 mod 放在 `MilkCum/Integration/` 下新建子文件夹。
2. **索引**：在 **Source/索引.md** 对应章节表格中新增一行，填写文件名、类型、主要函数/成员、功能简述。
3. **命名空间**：新文件命名空间与所在目录一致；GlobalUsings 已包含 Core.Settings、Core.Stats、Core.Utils、Core.Constants、Fluids.Lactation.*、Fluids.Shared.Data/Models/Helpers、Harmony、Integration.DubsBadHygiene。

---

## 四、相关文档

- 目录与类型明细：**Source/索引.md**
- 集成模块一览：**记忆库/docs/Integrations索引.md**
- 架构原则与重组建议：**记忆库/design/架构原则与重组建议.md**
