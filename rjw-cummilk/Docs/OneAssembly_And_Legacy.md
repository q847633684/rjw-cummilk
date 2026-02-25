# 合并为单一程序集 + 旧代码/框架备注

**已完成**：Cumpilation 已并入 EqualMilking（源码移至 `Source/EqualMilking/Cumpilation/`，由 EqualMilking 工程统一编译为 EqualMilking.dll；Cumpilation.csproj 已移除，HarmonyInit 不再单独创建 Harmony）。**内容已统一**：原 `Versions/1.6/Cumpilation` 与 `Versions/1.6/Integrations/Cumpilation` 已合并进 `Versions/1.6`（Defs、Patches、Languages、Mods），LoadFolders 已更新，上述目录已删除。

---

## 一、合并到单一框架（单 DLL）的方案

### 当前结构

| 程序集 | 输出位置 | 说明 |
|--------|----------|------|
| EqualMilking | `Assemblies/` | 主逻辑 + **Cumpilation**（精液/Cumflation/泄精/清洁/Slug 等）同一 DLL |
| RJW | `Integrations/RJW/Assemblies/` | 仅当 rim.job.world 激活时 LoadFolders 加载 |
| PipeSystem | `Integrations/PipeSystem/Assemblies/` | 仅当 VFE 激活时加载 |
| VME_HarmonyPatch | `Integrations/VME_HarmonyPatch/Assemblies/` | 仅当 Milk Expanded 激活时加载 |

### 目标：只保留一个主 DLL（EqualMilking.dll）

1. **把 Cumpilation 源码并入 EqualMilking 工程**
   - 在 `EqualMilking.csproj` 里用 `<Compile Include="..\Cumpilation\**\*.cs" />` 或逐个添加 `Source/Cumpilation/**/*.cs`，命名空间保持 `Cumpilation.*`，Def 里引用的类名不变。
   - 在 EqualMilking 的 Mod 入口里**不再**加载 Cumpilation.dll，只加载 EqualMilking.dll。
   - Cumpilation 当前依赖 RJW：合并后主 Mod 已依赖 RJW，在 EqualMilking 里加对 RJW.dll 的引用即可（与现在 About 的 modDependencies 一致）。
   - **Cumpilation 的 Harmony**：`Source/Cumpilation/HarmonyInit.cs` 里目前用 `new Harmony("vegapnk.cumpilation")` 并 `PatchAll()`。合并后二选一：
     - **推荐**：在 `EqualMilking.EqualMilking` 静态构造里先 `PatchAll(EqualMilking)`，再调用 Cumpilation 的 Patch 逻辑（把 HarmonyInit 的 PatchAll 移到由 EqualMilking 调用的一个方法里，或用同一 Harmony 实例对 Cumpilation 程序集里的类型 PatchAll），这样只有一个 Harmony 实例、一个 DLL。
     - 或者保留 `HarmonyInit` 在合并后的 DLL 里仍执行一次，仅把 ID 改为 `akaster.equalmilking.cumpilation` 之类，避免和已卸载的 vegapnk.cumpilation 冲突。
   - **构建**：不再生成、不再拷贝 Cumpilation.dll；LoadFolders 里若有按 Cumpilation 程序集存在才加载的逻辑，改为始终加载本 Mod 的 Defs（当前已是始终加载 Versions/1.6/Cumpilation Defs）。

2. **RJW / PipeSystem / VME 集成**
   - **方案 A（仍保留 3 个可选 DLL）**：维持现状，RJW/PipeSystem/VME 三个工程只在对应 Mod 激活时由 LoadFolders 加载；主 DLL 只含 EqualMilking + Cumpilation。这样改动最小，只是“把 Cumpilation 合并进主框架”。
   - **方案 B（全部进一个 DLL）**：把 `Source/RJW`、`Source/PipeSystem`、`Source/VME_Patch` 的 .cs 都并入 EqualMilking 工程；RJW/PipeSystem 依赖用“可选引用”（如不直接 Reference，运行时用 `AccessTools.TypeByName` 等判断再 Patch），这样只输出 EqualMilking.dll，三种集成都在同一程序集里，用 ModLister 判断是否启用对应 Mod 再打 Patch。RJW 为必选依赖，可直接引用；PipeSystem/VME 为可选，需反射或条件编译。

建议先做 **Cumpilation 并入 EqualMilking**，RJW/PipeSystem/VME 保持可选 DLL 或后续再并。

### 合并 Cumpilation 时的具体步骤（简要）

1. 在 `EqualMilking.csproj` 中加入 Cumpilation 的所有 .cs（保留目录结构或按命名空间放好），并添加对 RJW 的引用（路径可用 MSBuild 变量或相对路径，便于不同机器构建）。
2. 修改 `Source/Cumpilation/HarmonyInit.cs`：不再 `new Harmony("vegapnk.cumpilation")`，改为由 `EqualMilking` 传入的 Harmony 实例对 Cumpilation 命名空间下类型执行 Patch（或把 Cumpilation 的 Patch 集中到一个静态方法，由 EqualMilking 启动时调用）。
3. 删除或不再构建 `Source/Cumpilation/Cumpilation.csproj`，并确保发布/打包时只拷贝 EqualMilking.dll，不拷贝 Cumpilation.dll。
4. 检查 Defs 中所有 `Cumpilation.Gathering.FluidGatheringDef`、`Cumpilation.Leaking.CompProperties_DeflateBucket` 等：合并后类型在同一程序集，无需改 Def，仅确保从本 Mod 加载即可（已满足）。

---

## 二、旧代码 / 框架备注（是否需要删除）

### 建议删除或移除的项

| 位置 | 说明 | 建议 |
|------|------|------|
| **LoadFolders.xml** 中的 `Versions/1.6/CumpilationCommon` | 目录为空，没有任何文件 | **删除**该 `<li>Versions/1.6/CumpilationCommon</li>`，避免无意义加载项。 |
| **Versions/1.6/CumpilationCommon** 目录 | 空文件夹 | 可删除整个目录（若 LoadFolders 已删该项）。 |
| **XML 注释里 “When Cumpilation (vegapnk.cumpilation) is active”** | 表示“当独立 Cumpilation 模组激活时”，现已合并进本 Mod | **改为**“本 Mod 内已包含 Cumpilation；为 Cumpilation_Cum 添加 CompShowProducer”等，或删除这句过时说明。涉及文件：`Versions/1.6/Integrations/Cumpilation/Patches/Cumpilation_Cum_ShowProducer.xml`、`Cumpilation_Bucket_LinkBed.xml`。 |

### 建议修改但保留的项（旧框架/兼容）

| 位置 | 说明 | 建议 |
|------|------|------|
| **Cumpilation/HarmonyInit.cs** 的 `new Harmony("vegapnk.cumpilation")` | 原 Cumpilation 的 Harmony ID | 合并到单 DLL 后改为由 EqualMilking 用同一 Harmony 实例统一 Patch，或保留一次 PatchAll 但把 ID 改为例如 `akaster.equalmilking.cumpilation`，避免与已卸载的 vegapnk 冲突。**不删文件**，只改调用方式或 ID。 |
| **About.xml** 的 `<incompatibleWith><li>vegapnk.cumpilation</li></incompatibleWith>` | 禁止与旧版独立 Cumpilation 同时启用 | **保留**，防止用户同时开旧 Cumpilation 与本 Mod，造成重复逻辑/冲突。 |
| **CumpilationIntegration.cs** 注释 “When Cumpilation is loaded” | 原意是“当 Cumpilation 被加载时” | 改为“本 Mod 内 Cumpilation 已合并，为精液打产主并统一食用规则”。**不删**，只更新注释。 |
| **Languages 里 “when Cumpilation active”** | 如 EM.ProducerRestrictionsColumnTip 的 “Male (when Cumpilation active)” | 改为 “Male: who can eat my cum products” 等，不再提“Cumpilation 是否激活”（因为始终激活）。**不删**，只改文案。 |

### 可选清理（按需）

| 位置 | 说明 | 建议 |
|------|------|------|
| **Cumpilation/Settings** 的 `cumpilation_settings_menuname` 等 | Cumpilation 子设置菜单的键名 | 若希望设置完全并入 Equal Milking 一个界面，可后续把 Cumpilation 设置页并进去；否则**保留**，仅作文案/命名统一。 |
| **Docs/Cumpilation_RJWGenes_Integration.md** | 当时“独立 Cumpilation + 本 Mod Patch”的集成说明 | 标注为“历史文档：当前已合并为单 Mod”，或补充一节“现为单 Mod，Cumpilation 已内置”。**不必删**，避免以后看不懂历史设计。 |
| **RJW.csproj / PipeSystem.csproj 的 ../../Libs/Mods/rjw、Workshop 路径** | 本机/CI 的 DLL 路径 | 若合并为单 DLL 且 RJW 集成也进主工程，主工程保留对 RJW 的引用、路径可用变量；RJW/PipeSystem 的 csproj 若不再使用可**删除**，并相应删 LoadFolders 里对 Integrations/RJW、Integrations/PipeSystem 的加载。 |

### 不需要删除的

- **EqualMilking 里用 `AccessTools.TypeByName("Cumpilation.Leaking.xxx")`**：合并后类型在同一程序集，TypeByName 仍然有效，**无需删**。
- **Defs 里 Cumpilation.Gathering、Cumpilation.Leaking 等类名**：合并后仍由同一 DLL 提供，**无需改 Def**。
- **Versions/1.6/Cumpilation** 下所有 Defs/Patches/Mods：**保留**，这是“内容”而不是旧框架，合并后仍由本 Mod 加载。

---

## 三、小结

- **合并到一个框架**：把 Cumpilation 源码并入 EqualMilking 工程，输出一个 EqualMilking.dll（含 Cumpilation 命名空间）；RJW/PipeSystem/VME 可暂时保持可选 DLL，或后续再并入同一 DLL 并用条件 Patch。
- **建议删除**：LoadFolders 中的 `CumpilationCommon` 条目、空目录 `Versions/1.6/CumpilationCommon`；过时的 “vegapnk.cumpilation is active” 类 XML/注释改为“本 Mod 已包含 Cumpilation”。
- **建议保留并只做小改**：HarmonyInit 的调用方式或 Harmony ID、About 的 incompatibleWith、CumpilationIntegration 与语言里的“Cumpilation 激活”表述改为“精液/产主”描述。
- **不必删**：所有 Def、Cumpilation 命名空间下的类型引用、现有产主/食用规则逻辑；仅在做“单 DLL”时删 Cumpilation.csproj 的构建与输出。

如果你愿意，我可以按“只合并 Cumpilation 进 EqualMilking、其余不动”的步骤，给出对 EqualMilking.csproj 和 HarmonyInit 的具体修改片段（含你仓库里的路径）。
