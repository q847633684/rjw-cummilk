---
name: rimworld-def-xml
description: Check and complete RimWorld Mod Def/XML files against project conventions—Defs, LanguageData, DefInjected, and Patches. Use when editing or adding .xml in Defs, Languages, or Patches folders, or when the user asks to validate/fix RimWorld Def or translation XML.
---

# RimWorld Mod Def/XML 检查与补全

在编辑 RimWorld Mod 的 Def/XML 时，按下列规范检查并补全标签与多语言键。

## 1. Def 文件（Defs/*.xml）

- **根节点**：必须是 `<Defs>`，其子元素为具体 Def 类型（如 `HediffDef`、`ThoughtDef`、`ThingDef`）。
- **defName**：每个 Def 必须有唯一 `<defName>`；命名建议带 Mod 前缀（如 `EM_Prolactin_Tolerance`），PascalCase。
- **父级继承**：需要继承时使用 `ParentName="BaseDefName"`，再覆盖或追加字段。
- **缩进**：4 空格；列表项 `<li>` 与父级内容对齐。
- **注释**：用 `<!-- 说明 -->` 标注设计意图或规格引用；重要逻辑建议简短注释。
- **常见 Def 类型**：HediffDef（label, description, comps, stages）、ThoughtDef（thoughtClass/workerClass, stages, baseMoodEffect）、ThingDef、JobDef、RecipeDef 等；缺字段时按游戏要求补全，避免漏掉 `label`/`description` 导致游戏内显示 defName。

**检查清单**：
- [ ] 根为 `<Defs>`，子元素标签与 Def 类型一致
- [ ] 每个 Def 有且仅有唯一 `defName`
- [ ] 需要玩家可见的文本有 `label`/`description`（或由 DefInjected 提供）
- [ ] 列表/嵌套结构闭合正确，无未闭合标签

## 2. 语言 Keyed 文件（Languages/.../Keyed/*.xml）

- **根节点**：`<LanguageData>`，子元素为键值对：`<KeyName>显示文本</KeyName>`，键名与代码中引用一致（如 `EM.LactatingGain`）。
- **占位符**：使用 `{0}`、`{1}` 等，与 C# 中 `string.Format` 顺序一致。
- **XML 转义**：`<` → `&lt;`，`>` → `&gt;`，`&` → `&amp;`，属性或文本中的引号按需转义。
- **多语言对齐**：若存在 English 与 ChineseSimplified（或其他语言），确保两边的 Keyed 文件**键集合一致**；新增键时在**所有**语言文件中同步添加，缺键则补键或占位译文，避免键缺失导致显示键名。

**检查清单**：
- [ ] 根为 `<LanguageData>`，键名合法且与代码一致
- [ ] 占位符 `{0}` 等与代码参数顺序一致
- [ ] 所有语言文件中键集合一致（键不缺失、不多余）

## 3. DefInjected 文件（Languages/.../DefInjected/DefType/*.xml）

- **用途**：覆盖或补充已有 Def 的 `label`、`description`、`stages.*.label` 等，按路径注入。
- **路径格式**：`DefName.field` 或 `DefName.stages.0.label`（索引从 0 开始）；与 Def 中实际结构一致。
- **根节点**：`<LanguageData>`，子元素为 `<DefName.path>文本</DefName.path>`。
- **多语言对齐**：同上，若有多语言，同一 Def 的 DefInjected 键应在各语言中一致；缺的补译或占位。

**检查清单**：
- [ ] 路径与 Def 中节点对应（defName、stages 索引、字段名）
- [ ] 各语言 DefInjected 中同一 Def 的路径集合一致

## 4. Patch 文件（Patches/*.xml）

- **根节点**：`<Patch>`，子元素为 `<Operation Class="PatchOperation...">`（如 `PatchOperationAdd`、`PatchOperationRemove`、`PatchOperationReplace`、`PatchOperationSequence`）。
- **xpath**：目标必须准确指向 Def 节点，常用形式 `Defs/ThoughtDef[defName="DefName"]/子路径`；defName 与游戏内 Def 一致。
- **value / operations**：按 Operation 类型填写；Sequence 内用 `<operations><li Class="...">...</li></operations>`。
- **可读性**：复杂 Patch 可加简短 `<!-- -->` 说明意图。

**检查清单**：
- [ ] 根为 `<Patch>`，Operation 的 Class 与 xpath 正确
- [ ] xpath 中的 defName 与目标 Mod 的 Def 一致，子路径存在

## 5. 通用 XML 规范

- **编码**：文件保存为 UTF-8；若含 `<?xml version="1.0" encoding="utf-8"?>` 则保持一致。
- **闭合**：所有标签必须正确闭合；自闭合用 `/>`。
- **属性引号**：属性值一律用双引号。
- **路径**：技能与文档中提及文件路径时使用正斜杠 `path/to/file.xml`，避免反斜杠。

## 6. 执行流程（检查与补全）

1. **识别文件类型**：Defs → 1；Languages/.../Keyed → 2；Languages/.../DefInjected → 3；Patches → 4。
2. **按对应小节检查**：逐项跑检查清单，记录缺失或错误。
3. **补全**：缺的 `defName`、`label`、`description`、Keyed/DefInjected 键补全；多语言键对齐；Patch 的 xpath/value 修正。
4. **输出**：简要列出修改项（文件 + 补全/修正内容），便于用户确认。

## 补充参考

- 各 Def 类型可用字段与游戏行为见 [reference.md](reference.md)。
- 若项目内有 `Docs/` 下的设计文档（如“水池模型”“药品 Def 变量参考”），补全 Def 时优先遵循其中命名与数值约定。
