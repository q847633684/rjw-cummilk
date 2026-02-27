# rjw-cummilk Source 目录结构建议

## 一、当前结构概览

```
Source/
├── MilkCum/                 # 主项目（MilkCum.csproj）— 泌乳 + Cumpilation 精液
│   ├── Comps/              # 各类 Comp / Hediff
│   ├── Cumpilation/        # 精液相关（Bukkake, Cumflation, Leaking, Gathering, Oscillation, Reactions, Fluids, Common, Settings）
│   ├── Data/               # 数据
│   ├── Givers/             # WorkGiver / JobGiver（挤奶、哺乳）
│   ├── HarmonyPatches/     # 泌乳 + Cumpilation 的 Harmony
│   ├── Helpers/            # 扩展方法、常量、第三方集成（DubsBadHygiene 等）
│   ├── Jobs/               # JobDriver_EquallyMilk
│   ├── UI/                 # 设置、列、窗口、对话框
│   ├── EqualMilking.cs
│   ├── EqualMilkingSettings.cs
│   ├── EMDefOf.cs
│   ├── ThoughtWorker_*.cs
│   └── WorldComponent_EqualMilkingAbsorptionDelay.cs
├── RJW/                    # 另一项目（RJW.csproj）— 与 RJW 的集成
├── PipeSystem/             # 另一项目（PipeSystem.csproj）
└── VME_Patch/              # 另一项目（VME_HarmonyPatch.csproj）
```

**现状问题简述：**

- 根目录下既有“主功能”又有“集成”，主项目下 **泌乳（Milk）** 与 **精液（Cumpilation）** 混在同一层，仅用 Cumpilation 子文件夹区分。
- 根级文件（Entry、Settings、DefOf、ThoughtWorker、WorldComponent）未按功能分组，查找时需在根目录扫一遍。
- 命名空间与路径不完全一致：如 `EqualMilking.RJW` 在 `Source/RJW/`，Cumpilation 在 `EqualMilking/Cumpilation/`。

---

## 二、建议目标结构（仅整理 EqualMilking 主项目）

在 **不拆程序集、尽量少改 namespace** 的前提下，建议把 **Source/EqualMilking/** 按“功能域”分组，便于维护和查找。

### 方案 A：按功能域分组（推荐）

```
Source/
├── EqualMilking/                           # 主项目根
│   │
│   ├── Core/                               # 模组入口与全局
│   │   ├── EqualMilking.cs                 # Mod 入口
│   │   ├── EqualMilkingSettings.cs         # 设置与 Scribe
│   │   └── EMDefOf.cs                      # DefOf
│   │
│   ├── Milk/                               # 泌乳 / 挤奶 / 水池 / 哺乳
│   │   ├── Comps/                          # CompEquallyMilkable, Hediff*Lactating, Hediff_*
│   │   ├── Jobs/                           # JobDriver_EquallyMilk
│   │   ├── Givers/                         # WorkGiver_EquallyMilk, JobGiver_*Breastfeed
│   │   ├── Helpers/                        # ExtensionHelper, ChildcareHelper, Constants, Lang, DubsBadHygieneIntegration, MenuHelper
│   │   ├── HarmonyPatches/                 # Milking, HealthTabProducerRestrictions, MilkProductConsumptionPatch
│   │   ├── Thoughts/                       # ThoughtWorker_LongTimeNotMilked, MilkPoolFull, AddictionSatisfied, ProlactinWithdrawal
│   │   └── World/                          # WorldComponent_EqualMilkingAbsorptionDelay
│   │
│   ├── UI/                                 # 所有界面（保持不变）
│   │   ├── Widget_*.cs
│   │   ├── Window_*.cs
│   │   ├── Dialog_*.cs
│   │   └── PawnColumnWorker_*.cs
│   │
│   ├── Cumpilation/                        # 精液相关（保持现有子结构）
│   │   ├── Bukkake/
│   │   ├── Common/
│   │   ├── Cumflation/
│   │   ├── Fluids/
│   │   ├── Gathering/
│   │   ├── Leaking/
│   │   ├── Oscillation/
│   │   ├── Reactions/
│   │   └── Settings/
│   │
│   ├── Integration/                        # 主项目内的“集成”逻辑（可选：把 RJW 等编译进主程序集时放这里）
│   │   └── RJW/                            # Alert_FluidMultiplier, RJW.cs 等（若并入 EqualMilking 项目）
│   │
│   └── Data/                               # 数据类（若有）
│
├── RJW/                                    # 独立项目（若保持单独编译）
├── PipeSystem/
└── VME_Patch/
```

**分组含义：**

| 目录 | 职责 | 说明 |
|------|------|------|
| **Core** | 模组入口、全局设置、DefOf | 与具体玩法无关的“壳” |
| **Milk** | 泌乳、挤奶、水池、哺乳、耐受等 | 奶相关玩法与心情 |
| **UI** | 设置界面、列、窗口、对话框 | 统一入口，便于改 UI |
| **Cumpilation** | 精液：膨胀、溅射、泄漏、收集、振荡、反应等 | 保持现有子模块，只做路径不变或微调 |
| **Integration** | 可选，主程序集内的第三方集成 | 若 RJW 等代码并入 EqualMilking 项目可放此 |

**命名空间建议（可选、渐进）：**

- 保持现有 `EqualMilking`、`EqualMilking.Helpers`、`EqualMilking.UI`、`Cumpilation.*` 等不变，仅调整物理路径即可。
- 若希望路径与命名空间更一致，可逐步改为：
  - `EqualMilking.Core`（Core/）
  - `EqualMilking` 或 `EqualMilking.Milk`（Milk/ 下，Comps/Helpers 等可仍用 EqualMilking / EqualMilking.Helpers）
  - `EqualMilking.Integration.RJW`（Integration/RJW/）

---

### 方案 B：最小改动（只收拢根目录文件）

若不想新增 Core / Milk 等顶层目录，可只做“收拢根级文件”：

```
Source/EqualMilking/
├── Comps/
├── Cumpilation/        # 不变
├── Givers/
├── HarmonyPatches/
├── Helpers/
├── Jobs/
├── UI/
├── Data/
│
├── EqualMilking.cs
├── EqualMilkingSettings.cs
├── EMDefOf.cs
├── Defs/               # 新建：仅放“逻辑上属于 Def 的根级类型”（若有）
├── Thoughts/           # 新建：ThoughtWorker_*.cs 移入
└── World/              # 新建：WorldComponent_*.cs 移入
```

即：只新增 **Thoughts/** 和 **World/**，把散落在根目录的 ThoughtWorker 与 WorldComponent 移进去，其余不动。命名空间可保持不变（仅改文件路径与 csproj 包含）。

---

## 三、Cumpilation 内部是否再拆

当前 Cumpilation 已按功能分子目录（Bukkake、Cumflation、Leaking、Gathering、Oscillation、Reactions、Fluids、Common、Settings），**建议保持**。若后续某一块（如 Leaking）文件激增，再在该块下按“Comps / Jobs / Patches”等细分即可。

---

## 四、实施顺序建议

1. **先做方案 B**：新建 `Thoughts/`、`World/`，移动对应文件并更新 .csproj，保证编译通过。
2. **再考虑方案 A**：  
   - 新建 `Core/`，移入 `EqualMilking.cs`、`EqualMilkingSettings.cs`、`EMDefOf.cs`。  
   - 新建 `Milk/`，将 `Comps`、`Jobs`、`Givers`、`Helpers`、`HarmonyPatches` 移入，并把当前根目录下与泌乳直接相关的 ThoughtWorker / WorldComponent 归到 Milk/Thoughts、Milk/World（若已用方案 B，则从 Thoughts/、World/ 再迁到 Milk 下）。  
   - 若有“主程序集内 RJW 集成”代码，再建 `Integration/RJW` 并迁移。
3. 每次移动后跑一次编译并做一次简单游戏内测试（泌乳 + 精液相关操作）。

---

## 五、总结

| 方案 | 改动量 | 效果 |
|------|--------|------|
| **A** | 大（新建 Core/Milk/Integration，批量移动） | 功能域清晰，后续加功能时归属明确 |
| **B** | 小（仅 Thoughts/、World/） | 根目录更干净，几乎不影响现有命名空间与引用 |

建议：**短期采用方案 B，中长期逐步向方案 A 靠拢**；Cumpilation 保持现有子结构，不强行与 Milk 合并。
