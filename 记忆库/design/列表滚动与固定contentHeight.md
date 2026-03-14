# 列表滚动与固定 contentHeight

## 背景

RimWorld 的 `Widgets.BeginScrollView(viewRect, ref scrollPos, scrollContent, ...)` 要求调用方提供 `scrollContent` 的矩形，其中高度决定可滚动范围。若高度小于实际绘制内容，底部会被截断。

## 本 mod 中的用法

| 位置 | 内容高度来源 | 说明 |
|------|----------------|------|
| **Widget_AdvancedSettings** | 固定常量 `SectionScrollContentHeight = 2400f` | 健康/效率/联动三个 Tab 下由 Listing_Standard 动态绘制，无法在绘制前精确得到总高度，故用较大固定值 |
| Window_Search | `searchResults.Count * Text.LineHeight` | 按条目数计算，无截断风险 |
| Widget_MilkTagsTable | `productsToTags.Count * UNIT_SIZE` | 按条目数计算 |
| Widget_MilkableTable | `pawnDefs.Count() * UNIT_SIZE` | 按条目数计算 |
| Widget_GeneSetting | `gene_MilkTypes.Count * LINE_HEIGHT` | 按条目数计算 |
| Dialog_SelectBedForBucket | `(bedsInRoom + bedsElsewhere).Count * 32f + 64f` | 按床位数计算 |

## 当前策略

- 设置区（Widget_AdvancedSettings）使用 **2400f** 的固定高度（曾为 1200f），在常见分辨率与选项数量下应能覆盖全部内容。
- 若用户反馈在极高 DPI 或某一子 Tab 内容极多时仍被截断，可考虑：
  1. **提高常量**：将 `SectionScrollContentHeight` 调大（如 3600f）。
  2. **按内容估算**：若各子区块能提供“预估行数”或“最大高度”，可在 DrawSection 内按 mainTab/subTab 选用不同 contentHeight（仍为估算，非精确测量）。
  3. **两遍布局**：第一遍用 `GUI.enabled = false` 仅做测量（Listing_Standard 会推进 CurHeight），第二遍再正式绘制；实现成本较高且需注意焦点/交互。
  4. **虚拟滚动 / ListView**：RimWorld 若提供类似 API，可只绘制可见行以兼顾性能与任意长度列表；需查官方/社区 API。

## 参考

- UI 审阅结论：记忆库/design/UI审阅结论.md §4 列表滚动。
- 常量定义：Source/MilkCum/UI/Widgets/Widget_AdvancedSettings.cs `SectionScrollContentHeight`。
