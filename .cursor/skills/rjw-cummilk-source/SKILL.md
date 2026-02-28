---
name: rjw-cummilk-source
description: Apply rjw-cummilk (Equal Milking) C# source conventions when editing Source folder—namespaces, EMDefOf, translations, Harmony patches, settings, and Docs-driven design. Use when editing .cs files in rjw-cummilk/Source or when the user asks for mod code style or architecture for this project.
---

# rjw-cummilk Source 代码规范

在编辑 rjw-cummilk 的 `Source/` 下 C# 代码时，按下列规范检查与实现。

## 1. 命名空间与目录

- **命名空间**与文件夹一一对应，使用 **file-scoped namespace**（`namespace X;`，无花括号包裹）。
- 映射：`Source/MilkCum/Core` → `MilkCum.Core`；`Source/MilkCum/UI` → `MilkCum.UI`；`Source/MilkCum/Milk/Comps` → `MilkCum.Milk.Comps`；`Source/MilkCum/Milk/Helpers` → `MilkCum.Milk.Helpers`；`Source/MilkCum/Milk/Data` → `MilkCum.Milk.Data`；`Source/MilkCum/Milk/Givers` → `MilkCum.Milk.Givers`；`Source/MilkCum/Milk/Jobs` → `MilkCum.Milk.Jobs`；`Source/MilkCum/Milk/Thoughts` → `MilkCum.Milk.Thoughts`；`Source/MilkCum/Milk/HarmonyPatches` → `MilkCum.Milk.HarmonyPatches`；`Source/RJW` → `MilkCum.RJW`；`Source/PipeSystem` → `MilkCum.PipeSystem`。
- 新增类型放在与命名空间匹配的目录下；跨模块引用用 `using MilkCum.Core;` 等，不破坏分层。

## 2. Def 引用

- **本 mod 的 Def** 一律通过 **EMDefOf**（`MilkCum.Core.EMDefOf`）访问，例如 `EMDefOf.EM_Prolactin`、`EMDefOf.EM_Prolactin_Tolerance`、`EMDefOf.EM_ForcedBreastfeed`。新增 Def 时在 `EMDefOf.cs` 中增加对应静态字段，并保证 Defs 中有同名 defName。
- **原版 / 其他 mod 的 Def** 使用 `DefOf`（如 `HediffDefOf.Lactating`、`JobDefOf.Milk`、`ThingDefOf.Human`）或 `DefDatabase<T>.GetNamed("defName")`；若仅为取 label/description，可用 `"Milk".DefLabel<ThingDef>()` 等扩展（见 Lang.cs）。
- 不在 C# 中手写 defName 字符串重复引用本 mod Def；统一走 EMDefOf。

## 3. 翻译与文案

- **Keyed 文案**：代码中用 `"KeyName".Translate()` 或 `"KeyName".Translate(arg)`，KeyName 与 `Languages/.../Keyed/*.xml` 中键一致（如 `Equal_Milking`、`EM.LactatingGain`）；新增键时同步在 **English** 与 **ChineseSimplified**（及其他语言）的 Keyed 中补全。
- **Def 的 label/description**：运行时可通过 `Lang.SetDefaultLabel(def, text)`、`Lang.SetDefaultDesc(def, text)` 设置默认值；若当前语言存在 DefInjected 则保留注入不覆盖。静态/启动时集中设置放在 `MilkCum.Milk.Helpers.Lang` 的 `LoadDefTranslations()` 中。
- **拼接与复用**：常用词可用 `Lang.Join(...)`、原版 `DefLabel`/`DefDesc` 扩展（`Lang.cs`）；CJK 与空格由 `Lang` 处理，不手写硬编码多语言字符串。

## 4. Harmony 补丁

- 补丁类放在 `MilkCum.Milk.HarmonyPatches` 或集成模块对应目录（如 RJW 相关在 `MilkCum.RJW`）。
- 使用 `[HarmonyPatch(typeof(TargetType))]`、`[HarmonyPatch("MethodName")]` 或 `[HarmonyPatch(nameof(T.Method))]`；Prefix/Postfix 为 `public static`，参数与 Harmony 约定一致（`__instance`、`__result` 等）。
- 每个 Patch 方法用 `/// <summary>` 简要说明意图；若涉及设计文档（如「规格」「水池模型」）或版本行为（如 7.8、10.8-6），在注释中注明，便于与 `Docs/` 对应。
- 优先**挂接**原版/RJW 逻辑（替换 driver/comp class、在事件前后追加逻辑），避免重写整段游戏逻辑；与《游戏已接管变量与机制清单》一致。

## 5. 设置与存档

- 设置项集中在 **EqualMilkingSettings**（`MilkCum.Core`）；新增选项时加静态字段或委托到子对象（如 `MilkRiskSettings`），并在 `ExposeData()` 中 `Scribe_Values.Look` / `Scribe_Collections.Look`。
- **存档键**使用 `"EM.XXX"` 形式并保持稳定；修改键会破坏旧存档兼容，需注释说明或做迁移。
- UI 中需要显示的设置文案使用 Keyed 键（如 `EM.LactatingGainDesc`），不在 C# 里写死长句。

## 6. 与 Docs 的对应

- **《游戏已接管变量与机制清单》**：原版/RJW 已接管的机制（Lactating Def、JobDef.Milk、ChemicalDef、Need_Chemical、耐受/成瘾由 XML 与游戏驱动等）**不再在 C# 中重算**；只读或挂接，不重复实现。
- **《参数联动表》**：高级设置中参数的影响与联动以该表为准；新增或修改参数时更新表或在该文档中注明，并在代码注释中引用参数名。
- **《泌乳系统逻辑图》**：泌乳端到端流程、L/双池/耐受/进水/衰减/选侧等逻辑的唯一文档；涉及池、流速、公式时与之一致，注释可写「见 Docs/泌乳系统逻辑图」。
- **《药品Def变量参考》《参数联动表》**：药品 Def 字段、设置参数影响与联动；新增或修改时同步更新对应 Docs。

## 7. 检查清单（编辑后）

- [ ] 命名空间与文件路径、EMDefOf 使用正确
- [ ] 新增/修改的 Keyed 键在 English 与 ChineseSimplified 中已同步
- [ ] Harmony 仅做必要挂接，未重复实现游戏已接管逻辑
- [ ] 设置与 Scribe 键稳定、与 Docs 参数命名一致
- [ ] 重要设计决策在注释中引用 Docs 或规格

## 补充参考

- 命名空间与类型分布、Lang 扩展用法见 [reference.md](reference.md)。
- Def/XML 规范见 **rimworld-def-xml** 技能（若已安装）。
