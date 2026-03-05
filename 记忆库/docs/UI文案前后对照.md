# UI 文案前后对照

用于改 UI 文时的「当前 → 建议」对照，便于逐项采纳或调整。仅列与泌乳/奶池/健康页悬停/设置页相关的 key 与代码硬编码。

---

## 已采纳（2025-03 实施）

- **术语统一**：中文「产奶效率」→「流速」；`EM.MilkFlowPerDay`、`EM.MilkFlowValuePercent`、`EM.PoolBreastSideFlow`、`EM.PoolBreastPairSummary`、`EM.PoolPairEfficiencyHeader`、`EM.PoolLeftEfficiencyHeader`、`EM.PoolRightEfficiencyHeader`、`EM.PoolBreastFlowEfficiency` 已按上表修改。
- **哺乳期悬停微调**：`EM.LactatingStateFullShrinking`、`EM.MilkFlowStoppedFull` 已缩短；`EM.PoolPairFlowLine` 已改为「{0} 流速：{1}（左：{2}；右：{3}）」。
- **设置页**：`EM.DefaultFlowMultiplierForHumanlikeDesc` 已改为「所有单位奶池流速倍率」。
- **硬编码冒号**：新增 `EM.PoolBreastLabelSuffix`（中文「：」、英文 ": "），`Milking.cs` 中 `BuildBreastPairBlock` 已改为使用该 key，不再写死全角冒号。

以下为采纳前的对照表，保留作日后微调参考。

---

## 两处悬停展示示例（中文）

游戏内健康页上，只有这两类行会显示本 mod 的奶池/泌乳详情；其它行（如「药物泌乳负担」「泌乳增益」等）不会追加下面内容。

### 1. 乳房体型 hediff 悬停（如「人类乳房 (超巨大)」）

悬停在该行的**乳房体型 hediff**（人类乳房、动物乳房等）时，在原有 TipString 下方追加一整块，格式如下（示例数值）：

```
【人类乳房（超巨大）】

储量
    总奶量：1.20 / 2.00（60%）
    扩展容量：2.60

流速
    总流速：0.45 / 天

左乳
    奶量：0.62 / 1.00（62%）
    流速：0.22 / 天
    容量上限：1.30
只有 DevMode 才显示
    生产机制:
    乳房体积 ×1.8
    基因 ×1.20
    设置 ×1.25
    状态 ×1
    驱动 ×1
    饥饿 ×1
    压力 ×1
    喷乳反射 ×1

右乳
    奶量：0.58 / 1.00（58%）
    流速：0.23 / 天
    容量上限：1.30
只有 DevMode 才显示
    生产机制:
    乳房体积 ×1.8
    基因 ×1.20
    设置 ×1.25
    状态 ×1
    驱动 ×1
    饥饿 ×1
    压力 ×1
    喷乳反射 ×1
```

- 第一行：`EM.PoolBreastSectionHeader`（本节标题，即该 hediff 的 LabelCap）。
- 第二行：`EM.PoolBreastPairSummary`（总奶量、基础容量、撑大容量、该对总流速）。
- 左/右乳各四行：标题用 `EM.PoolLeftBreast` / `EM.PoolRightBreast` + `EM.PoolBreastLabelSuffix`，接着奶量、容量、流速、因子括号。

---

### 2. 哺乳期 hediff 悬停（如「哺乳期 (天数: 10.0, 奶量: 0.51%)」）

悬停在该行的**哺乳期 hediff**时，`CompTipStringExtra` 输出多行汇总（示例数值）：

```
产奶中

储量
    总奶量：1.02 / 2.00（51%）
    扩展容量：2.60

流速
    总流速：0.45 / 天

产物
    人类奶：x0.51
    乳汁质量：100%

消耗
    额外营养：0.12 / 天

周期
    剩余天数：10.0
    等效剂量：1.0
```

- **状态**：产奶中 / 池满（建议挤奶）/ 池满回缩中（吸收补充饱食度）/ 或因饥饿红色提示。
- **池与可产**：`EM.PoolTotalMilkCapacityFull`；若开启乳汁质量则 `EM.MilkQuality`；奶 Def 与可产数量。
- **流速与营养**：未满时显示 `EM.MilkFlowValuePercent`、各对 `EM.PoolPairFlowLine`、额外营养/天；池满时显示「池已满，无流速」，回缩时显示回缩吸收。
- **时间**：剩余天数或永久。
- **等效剂量**：由 L 推算的标准剂量数。

---

## 1. 术语统一（奶量 / 容量 / 流速）

约定：**池内当前量 = 奶量**，**池上限 = 容量**，**单位时间产出 = 流速**（避免「产奶效率」与「效率」混用）。

| Key | 位置 | 当前（中文） | 建议（中文） | 当前（英文） | 建议（英文） |
|-----|------|--------------|--------------|--------------|--------------|
| EM.MilkFlowPerDay | 哺乳期悬停、流速行 | 产奶效率：{0}/天 | 流速：{0}/天 | Milk flow: {0}/day | 保持 |
| EM.MilkFlowValuePercent | 哺乳期悬停 | 产奶效率：{0}({1})/天 | 流速：{0}({1})/天 | Milk flow: {0}({1})/day | 保持 |
| EM.PoolBreastSideFlow | 乳房体型悬停·左/右乳块 | 效率：{0}/天 | 流速：{0}/天 | Flow: {0}/day | 保持 |
| EM.PoolBreastPairSummary | 乳房体型悬停·总览行 | 总:奶量：{0}/容量：{1}({2})/效率:{3}/天 | 总：奶量 {0} / 容量 {1}({2}) / 流速 {3}/天 | Total: milk {0} / cap {1}({2}) / flow {3}/day | 保持 |
| EM.PoolPairEfficiencyHeader | （若仍使用） | 该乳产奶效率：{0}({1}%)/天 | 该对流速：{0}({1}%)/天 | Pair milk flow: {0}({1}%)/day | 保持 |
| EM.PoolLeftEfficiencyHeader | （若仍使用） | 左乳产奶效率：{0}({1}%)/天 | 左乳流速：{0}({1}%)/天 | Left milk flow: ... | Left flow: ... |
| EM.PoolRightEfficiencyHeader | （若仍使用） | 右乳产奶效率：{0}({1}%)/天 | 右乳流速：{0}({1}%)/天 | Right milk flow: ... | Right flow: ... |
| EM.PoolBreastFlowEfficiency | （若仍使用） | 左乳效率：... 右乳效率：... | 左乳流速：... 右乳流速：... | Left: ... Right: ... | 保持 |

---

## 2. 健康页·乳房体型悬停（BuildBreastPairBlock）

| Key | 当前（中文） | 建议（中文） | 当前（英文） | 建议（英文） |
|-----|--------------|--------------|--------------|--------------|
| EM.PoolBreastSectionHeader | 【{0}】 | 【{0}】 | [{0}] | 保持 |
| EM.PoolBreastPairSummary | 见上表 | 总：奶量 {0} / 容量 {1}({2}) / 流速 {3}/天 | 见上表 | 保持 |
| EM.PoolLeftBreast | 左乳 | 左乳 | Left | 保持 |
| EM.PoolRightBreast | 右乳 | 右乳 | Right | 保持 |
| EM.PoolBreastSideMilk | 奶量：{0} ({1}) | 奶量：{0} ({1}) | Milk: {0} ({1}) | 保持 |
| EM.PoolBreastSideCap | 容量：{0}({1}) | 容量：{0}({1}) | Cap: {0}({1}) | 保持 |
| EM.PoolBreastSideFlow | 效率：{0}/天 | 流速：{0}/天 | Flow: {0}/day | 保持 |
| EM.PoolEfficiencyFactorsBracket | （{0}） | （{0}） | ({0}) | 保持 |

**代码硬编码**（`Milking.cs` → `BreastPoolTooltipHelper.BuildBreastPairBlock`）：

| 位置 | 当前 | 建议 |
|------|------|------|
| 左/右乳标题后换行 | `AppendLine("：")`（全角冒号硬编码） | 新增 key 如 `EM.PoolBreastLabelSuffix` = 「：」/ ": "，或把「左乳：」「右乳：」做成一个 key `EM.PoolLeftBreastWithColon` / `EM.PoolRightBreastWithColon`，代码用 key 拼接 |

---

## 3. 健康页·哺乳期 hediff 悬停（CompTipStringExtra）

| Key | 当前（中文） | 建议（中文） | 当前（英文） | 建议（英文） |
|-----|--------------|--------------|--------------|--------------|
| EM.LactatingStateProducing | 产奶中（池满度 {0}） | 产奶中（池满度 {0}） | Producing (pool {0} full) | 保持 |
| EM.LactatingStateFullShrinking | 池满，回缩中（未溢出部分会补充饱食度） | 池满回缩中（吸收补充饱食度） | Pool full, shrinking (reabsorbed milk adds to food) | 保持 |
| EM.LactatingStateFull | 池满（建议挤奶） | 池满（建议挤奶） | Pool full (consider milking) | 保持 |
| EM.MilkFlowStoppedFull | 当前不进水（池已满） | 池已满，无流速 | No inflow (pool full) | 保持 |
| EM.PoolTotalMilkCapacityFull | 总奶量：{0} ({1}) / 总容量 {2}({3}) | 总奶量：{0} ({1}) / 总容量：{2}({3}) | Total milk: {0} ({1}) / Capacity {2}({3}) | 保持 |
| EM.ReabsorbedNutritionPerDay | 回缩吸收：+{0} 营养/天 | 回缩吸收：+{0} 营养/天 | Reabsorbed (pool shrinking): +{0} nutrition/day | 保持 |
| EM.PoolRemainingDays | 剩余天数 | 剩余天数 | Remaining days | 保持 |
| EM.PoolPairFlowLine | {0}: {1}(左乳：{2}；右乳：{3}) | {0} 流速：{1}（左：{2}；右：{3}) | {0}: {1}(Left: {2}; Right: {3}) | 保持 |

---

## 4. Letter / Mote / Alert

| Key | 当前（中文） | 建议（中文） | 当前（英文） | 建议（英文） |
|-----|--------------|--------------|--------------|--------------|
| EM.FullPoolLetterTitle | 需要挤奶 | 需要挤奶 | Needs milking | 保持 |
| EM.FullPoolLetterText | {0} 的奶池已满超过一天，建议挤奶以免不适或溢出。 | 保持 | {0} has been at full milk pool for over a day; consider milking to avoid discomfort or overflow. | 保持 |
| EM.MilkOverflowMote | 奶水溢出 | 奶水溢出 | Milk overflow | 保持 |
| EM.AlertLactatingButFluidZero | 泌乳期但 RJW 胸部流体倍率为 0… | 保持 | Lactating but RJW breast fluid multiplier 0; use Dev Mode, RJW: Edit parts. | 保持 |

---

## 5. 设置页（选列，长描述可后续单独缩短）

| Key | 当前（中文） | 建议（中文） |
|-----|--------------|--------------|
| EM.SectionDesc_BreastPool | 第一项「泌乳期增大 RJW 乳房」控制左右池容量与流速来源… | 可拆为「简短」+「详细」或保持，后续单独改 |
| EM.DefaultFlowMultiplierForHumanlike | 流速倍率：{0} | 保持 |
| EM.DefaultFlowMultiplierForHumanlikeDesc | 所有单位的产奶流速倍率（1=不变）… | 可改为「所有单位奶池流速倍率（1=不变）」与术语统一 |

---

## 6. 实施顺序建议

1. **先做**：术语统一（流速/奶量/容量）→ 改 `EM.MilkFlowPerDay`、`EM.MilkFlowValuePercent`、`EM.PoolBreastSideFlow`、`EM.PoolBreastPairSummary` 等中文 key。
2. **然后**：去掉硬编码冒号 → 新增 `EM.PoolLeftBreastWithColon` / `EM.PoolRightBreastWithColon`（或一个通用 suffix key），改 `Milking.cs` 使用 key。
3. **可选**：哺乳期悬停 `EM.MilkFlowStoppedFull`、`EM.LactatingStateFullShrinking` 等缩短或语气微调；设置页长描述拆短/详细。

采纳某一行时，在 **Languages/ChineseSimplified/Keyed/lang.xml** 与 **Languages/English/Keyed/lang.xml** 中改对应 `<key>` 文本即可；代码仅在一处（冒号）需改为使用新 key。
