# UI 审阅结论

对 rjw-cummilk 的 UI 层（Widgets、Windows、Dialogs、Tables、FloatMenu）的代码审阅摘要。对照：记忆库/docs/UI文案前后对照、EM.Keyed 翻译键、Lang 用法。

---

## 1. 结构概览

| 类型 | 位置 | 说明 |
|------|------|------|
| 主设置入口 | `MilkCumSettings.DoWindowContents` | 5 个主 Tab，子 Tab 随主 Tab 变化；内容由各 Widget 的 Draw/DrawSection 提供 |
| 主 Tab 列表 | 产奶与体液 / 哺乳 / 健康与风险 / 效率与界面 / 联动与扩展 |
| 高级设置块 | `Widget_AdvancedSettings` | 通过 `DrawSection(inRect, mainTab, subTab)` 按 MainTabIndex 与子 Tab 常量分发 |
| 产奶者表格 | `MainTabWindow` + `Milk_PawnTable` | 主界面按钮筛选（全部/殖民者/囚犯等），表格列由 PawnColumnWorker_* 提供 |
| 弹窗 | `Window_Search`、`Window_ProducerRestrictions`、`Dialog_GeneConfig`、`Dialog_SelectBedForBucket` |
| 浮动菜单 | `FloatMenuOptionProvider_InjectLactationDrug`、`FloatMenuOptionProvider_Breastfeed` |

---

## 2. 已做得好的点

- **翻译键**：设置页、区块标题、提示均使用 `EM.xxx`.Translate()，无大段硬编码文案。
- **Tooltip 回退**：`string.IsNullOrEmpty(t) ? "EM.xxx" : t` 在翻译缺失时回退为 key，避免空白。
- **Lang 与 EM 分工**：通用词（Cancel、Confirm、Colonist、Animal 等）用 Lang；本 mod 专属用 EM.*，一致。
- **子 Tab 文案**：主 Tab 用 EM.Tab.*，子 Tab 用 EM.SubTab.* 或 Lang.Colonist 等，统一。
- **Window_ProducerRestrictions**：标题与说明均用 EM.*，且使用 Lang.Allow/Forbid 等，风格统一。

---

## 3. 问题与建议

### 3.1 死代码：Widget_AdvancedSettings.Draw()（已修复）

- **现象**：`Widget_AdvancedSettings` 内约 130 行的 `Draw(Rect inRect)` 方法从未被调用；`MilkCumSettings.DoWindowContents` 只调用 `DrawSection(contentRect, mainTab, subTab)`。
- **已做**：已删除 `Draw()` 方法，仅保留注释说明改用 DrawSection。

### 3.2 未使用变量

- **位置**：`Widget_AdvancedSettings.cs` 约 339 行（Draw 内）`string prolactinLabel = MilkCumDefOf.EM_Prolactin?.label ?? "Prolactin";`
- **说明**：`prolactinLabel` 赋值后未使用；且该段处于已不使用的 `Draw()` 内，删除 Draw 时一并消失。若保留 Draw，建议删除该行。

### 3.3 硬编码回退文案

- **Widget_MilkableTable.cs**：表头回退已改为 `HediffDefOf.Lactating?.label ?? "EM.Lactating".Translate()`，并增加 Keyed `EM.Lactating`。（**已修复**）
- **Widget_MilkableTable**：`DefDatabase<ThingDef>.GetNamedSilentFail("Milk")` — 原版 defName 回退，合理。

### 3.4 窗口标题（已修复）

- **Window_Search**、**Dialog_GeneConfig**、**Window_ProducerRestrictions**：已设置 `optionalTitle`，并增加/使用 Keyed（EM.WindowSearchTitle、EM.GeneConfigTitle、EM.ProducerRestrictionsTitle）。

### 3.5 魔术数字与字符串（已修复）

- **主 Tab**：已引入 `MainTabIndex` 枚举，`DoWindowContents` 与 `GetSubTabs()` 使用 `(int)MainTabIndex.xxx`；`DrawSection` 传参同。
- **子 Tab**：`Widget_AdvancedSettings` 内已用命名常量（SubTabHealth_*、SubTabEfficiency_*、SubTabIntegration_*）替代 0、1、2、3。
- **效率区高度**：`IdentitySectionHeight = 280f` 已提为局部常量。
- **Widget_MilkableTable**：`ProductButtonPadding` 常量已存在。

### 3.6 空引用与健壮性

- **PawnColumnWorker_MilkFullness**：已先取 hediff 再 `hediff?.TryGetComp<>()`，null 时返回 "-"；并已添加 GetHeaderTip。其他列（MilkRemainingDays、MilkEquivalentDose、Lactating）已补 GetHeaderTip，表头悬停一致。

### 3.7 文案与文档

- 记忆库 **docs/UI文案前后对照** 中术语（流速/奶量/容量）与 Keyed 已对齐部分无需在 UI 审阅中重复修改；若后续在 UI 中新增「效率」等词，建议继续与文档保持一致。

---

## 4. 可选改进（已实施部分）

- **Window_Search**：已增加 `EM.SearchItemsPlaceholder` Keyed，并在搜索框上方显示灰字占位提示。（**已做**）
- **Dialog_GeneConfig**：已加类注释：若后续加项可改为可滚动或按内容计算高度。
- **Widget_BreastfeedSettings.Draw()**：已删除未使用的 `Draw(Rect)` 及 `tabIndex` 字段；主设置仅用 `DrawOverview` 与 `DrawTab`。
- **Widget_AdvancedSettings**：已删除未使用的 `_advancedScrollPosition` 字段。
- **奶标签表头**：已为表头区域绑定 `EM.MilkTagsTableHeaderDesc` Tooltip。
- **RJW 空子 Tab**：未安装 RJW 时联动子 Tab「RJW」显示「需要 RJW 模组」灰字。
- **列表滚动**：设置区唯一固定高度在 `Widget_AdvancedSettings`，已改为命名常量 `SectionScrollContentHeight = 2400f`（原 1200f），并加注释说明：若遇极高分辨率或内容截断反馈，可考虑按内容计算高度或 ListView/虚拟滚动（见 记忆库/design/列表滚动与固定contentHeight.md）。其余列表已按条目数×行高计算，无固定截断风险。

---

## 5. 逐像素布局审查（已修复）

对 Rect、滚动区、间距与裁切做逐像素核对后的结论与修改。

### 5.1 Window_Search

- **问题**：列表绘制起点为 `inRect.y + UNIT_SIZE*2 + Text.LineHeight`，而 `BeginScrollView` 的可见区（outRect）从 `inRect.y + UNIT_SIZE*3 + Text.LineHeight` 开始，导致首行落在可见区上方，滚动为 0 时**第一行被裁掉**。
- **修复**：统一「列表顶」为 `listTop = inRect.y + Text.LineHeight + UNIT_SIZE`；outRect 与 contentRect 均从 `listTop` 起算，listingRect 也从 `listTop` 起绘，首行与滚动区顶部对齐；content 高度用 `Mathf.Max(1f, searchResults.Count * Text.LineHeight)` 避免空列表时 0 高。

### 5.2 Widget_MilkableTable

- **问题**：`scrollRect` 的 y 设为 `tableRect.y + UNIT_SIZE`，内容整体比 view（tableRect）下移一行；view 顶部空一行，**第一条数据行在初始滚动时被裁掉**。
- **修复**：`scrollRect.y = tableRect.y`，内容高度仍为 `pawnDefs.Count() * UNIT_SIZE`；循环内 `y_Offset` 改为从 `tableRect.y - UNIT_SIZE` 起，先 `y_Offset += UNIT_SIZE` 再绘制，使首行落在 `tableRect.y`，与 view 顶部对齐。

### 5.3 Window_ProducerRestrictions

- **问题**：行内标签宽度为 `row.width - h - ButtonWidth - 12f`（约 width - 164）；窗口很窄（如 &lt;164）时可能 ≤0，引发异常或裁切异常。
- **修复**：标签宽度改为 `Mathf.Max(0f, row.width - h - ButtonWidth - 12f)`，两处列表（assigned / unassigned）均已应用。

### 5.4 其他结论（未改代码）

- **Dialog_GeneConfig**：InitialSize 280×450，iconRect 128²、ButtonSize (120,32)、lineRect 与 cancel/confirm 的 y 计算一致；内容区与底部按钮间有留白，当前尺寸下无重叠。
- **MilkCumSettings**：主/子 Tab、contentRect、IdentitySectionHeight、belowRect/geneRect/devRect 的 y 递进正确。
- **常量**：`Constants.UNIT_SIZE = 32f`，与 RimWorld 常用 24f 不同；各 UI 已统一使用该常量。

---

## 6. 审阅范围说明

- **已看**：MilkCumSettings 设置入口、Widget_AdvancedSettings、Widget_MilkableTable、Window_Search、Window_ProducerRestrictions、Dialog_GeneConfig、PawnTable_Main、Milk_PawnTable、PawnColumnWorker_MilkFullness/MilkType/Lactating、FloatMenuOptionProvider_InjectLactationDrug、记忆库 UI 文案文档与 EM Keyed 使用情况。
- **未做**：无障碍/色盲、与 RimWorld 各分辨率下的截图对比。**已做**：逐像素布局审查（见 §5），并已修复 Window_Search / Widget_MilkableTable / Window_ProducerRestrictions 三处裁切与窄窗问题。

更新：主/子 Tab 已用 MainTabIndex 与子 Tab 常量；RJW 空 Tab 提示、ProducerRestrictions 标题、奶标签表头 Tooltip、哺乳 DrawNutritionEnergyBlock 复用、IdentitySectionHeight、四列 GetHeaderTip、EM.Lactating 表头回退、SearchItemsPlaceholder、删除 BreastfeedSettings.Draw 与 _advancedScrollPosition、Dialog_GeneConfig 注释已实施。若再改 Tab 结构请同步本条目与 Source/索引.md。
