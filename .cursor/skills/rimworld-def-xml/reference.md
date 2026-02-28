# RimWorld Def/XML 参考

## Def 类型常用字段（简要）

| Def 类型 | 必选/常用字段 | 说明 |
|----------|----------------|------|
| HediffDef | defName, label, description, hediffClass | comps, stages, maxSeverity, initialSeverity, isBad；继承用 ParentName |
| ThoughtDef | defName, stages (label, description, baseMoodEffect) | thoughtClass 或 workerClass；durationDays（记忆类）；hediff（Hediff 驱动） |
| ThingDef | defName, label, description | category, thingClass, statBases；建筑/家具等需 graphic、size、building |
| JobDef | defName, driverClass, reportString | 通常由 Mod 代码引用，reportString 可 Keyed |
| RecipeDef | defName, label, jobDef, workAmount | ingredients, products, targetCount 等 |
| NeedDef | defName, label | needClass, baseLevel, fallPerDay 等 |
| ChemicalDef | defName, label | 化学物质定义，成瘾/耐受用 |
| PawnColumnDef | defName, label, workerClass | 表格列 |
| PatchOperation* | (在 Patch 根下) xpath, value 或 operations | 见 SKILL 第 4 节 |

## LanguageData 路径（DefInjected）

- 格式：`DefName.element` 或 `DefName.list.index.element`（如 `EM_Prolactin_Tolerance.stages.0.label`）。
- 常用注入目标：`label`、`description`、`stages.0.label`、`stages.1.description` 等；与 Def 内 XML 结构一一对应。
- 游戏加载时 DefInjected 会覆盖 Def 中同路径的默认值，因此仅写要覆盖的路径即可。

## Patch 常用 Operation

- **PatchOperationAdd**：xpath 指向父节点，value 为要添加的 XML 片段。
- **PatchOperationRemove**：xpath 指向要删除的节点。
- **PatchOperationReplace**：xpath 指向被替换节点，value 为替换内容。
- **PatchOperationSequence**：operations 下多个 li，按序执行；可选 success 控制后续是否继续。

## 多语言键对齐

- 以 English 为基准时，其他语言（如 ChineseSimplified）应包含与 English 相同的键集合；可多出键，但不宜少键。
- 新增 Keyed 或 DefInjected 时：在**每个** Languages/.../Keyed（或 DefInjected）中同步添加同一键，值可为占位译文后再细修。
