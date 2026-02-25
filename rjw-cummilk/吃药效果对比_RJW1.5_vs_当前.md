# 吃药效果对比：RJW 1.5 (Lact-X) vs 当前版本 (EM_Prolactin)

## 一、RJW 1.5 (Lact-X) 吃药后效果

### 1. 一次用药触发的 outcomeDoers（纯 XML，无 C# 补丁）

| 顺序 | 类型 | 效果 |
|------|------|------|
| 1 | GiveHediff | **Lactating_Drug**（自定义 hediff，initialSeverity 0.3，带 `toleranceChemical>Lact-X`，高耐受时效果减弱） |
| 2 | GiveHediff | **Lact-XHigh** severity 0.75（“high”状态，带 toleranceChemical） |
| 3 | OffsetNeed | **Joy +0.4**（带 toleranceChemical） |
| 4 | GiveHediff | **Lact-XTolerance** +0.044（`divideByBodySize>true`，体型大的人加得少） |

### 2. ingestible 直接属性

- `joyKind`: Chemical，`joy`: 0.40（原版“化学快感”条会涨）

### 3. 成瘾/化学 (CompProperties_Drug)

- chemical: Lact-X，addictiveness 0.04，minToleranceToAddict 0.03，existingAddictionSeverityOffset 0.20  
- **needLevelOffset 1**（满足一次药会多补一点需求）  
- **有过量**：overdoseSeverityOffset (0.18~0.35)，largeOverdoseChance 0.01  

### 4. “High”效果（Lact-XHigh hediff）

- 疼痛 ×0.9，意识 -10%，**操纵 / 移动 上限 50%**（短期削弱）
- 对应 Thought **Lact-XHigh**：+5 心情（“I feel like I can fly”）
- 严重度每天 -1，很快消失  

### 5. 戒断

- **Lact-XWithdrawal** 心情 **-15**
- 戒断阶段：疼痛×**3**，休息 +0.3，饥饿 +0.5，**意识 -20%**，**Eating -20%**  

### 6. 耐受与长期风险

- Lact-XTolerance：-0.015/天
- 高耐受：**胃部** ChemicalDamageModerate，minSeverity 0.50，MTB 120 天  

### 7. 泌乳机制（1.5 独有）

- **Lactating_Drug**：自定义 hediff，严重度每天 **-0.1**，会自然消退，需约每 10 天补一次药维持
- 与 Lact-X 化学耐受联动，耐受高时药效减弱  

---

## 二、当前版本 (EM_Prolactin) 吃药后效果（已加入短期负面 + 过量 + 耐受联动）

### 1. 一次用药触发的效果

| 来源 | 效果 |
|------|------|
| XML outcomeDoers | ① GiveHediff **Lactating** +0.5（**耐受联动**） ② GiveHediff **EM_Prolactin_High** +0.75（短期兴奋，**耐受联动**） ③ GiveHediff **EM_Prolactin_Tolerance** +0.044（**divideByBodySize**） ④ OffsetNeed **Joy** +0.3（**耐受联动**） |
| Harmony 补丁（仅当 hediffDef==Lactating 时执行一次） | 成瘾判定：4%×(1+耐受) |

### 2. 短期「兴奋」效果（EM_Prolactin_High）

- 愉悦：ThoughtWorker **EM_Prolactin_HighThought** +5 心情（持有该 hediff 时）
- 负面：疼痛×0.9，意识 -10%，**操纵 / 移动 上限 50%**
- 严重度每天 -1，短期存在

### 3. 成瘾/化学 (CompProperties_Drug)

- chemical: EM_Prolactin_Chemical，addictiveness 0.04，minToleranceToAddict 0.03，existingAddictionSeverityOffset 0.2  
- **有过量**：overdoseSeverityOffset (0.18~0.35)，largeOverdoseChance 0.01  

### 4. 戒断

- **EM_Prolactin_Withdrawal** 心情 **-12**
- 戒断阶段：疼痛×**2**，休息 +0.3，饥饿 +0.5，**意识 -15%**  

### 5. 耐受与长期风险

- EM_Prolactin_Tolerance：-0.015/天，**divideByBodySize**，高耐受有**肾脏**损伤风险  

### 6. 泌乳机制

- 原版 **Lactating** +0.5，**带耐受联动**（高耐受时当次泌乳效果减弱）  

---

## 三、主要区别汇总

| 项目 | RJW 1.5 (Lact-X) | 当前 (EM_Prolactin)（已更新） |
|------|-------------------|----------------------|
| 愉悦/心情 | Hediff Lact-XHigh +5 + Joy 0.4 | Hediff EM_Prolactin_High +5（ThoughtWorker）+ Joy 0.3 |
| 短期负面 | 操纵/移动 50%、意识 -10% | **已对齐**：操纵/移动 50%、意识 -10% |
| 过量 | 有 | **已添加**：overdoseSeverityOffset、largeOverdoseChance |
| 耐受联动 | 泌乳/High/Joy 带 toleranceChemical | **已添加**：泌乳、High、Joy 均带 toleranceChemical |
| 耐受/次 | +0.044，divideByBodySize | **已对齐**：+0.044，divideByBodySize |
| 戒断 | -15 心情，疼痛×3，意识 -20%，Eating -20% | -12 心情，疼痛×2，意识 -15%（未加 Eating） |
| 泌乳 hediff | 自定义 Lactating_Drug，会衰减 | 原版 Lactating，不改为 1.5 体系 |

---

## 四、哪个更好？建议方案

### 1. 结论简述

- **1.5**：纯 XML、有“high”负面、戒断更狠、有过量、泌乳可衰减且与耐受联动，更偏“硬核药物”体验。  
- **当前**：实现更简单（补丁只做“一次用药一次心情+成瘾”），愉悦略高、无短期负面、戒断略轻、无过量，更偏“温和药物”体验。  

没有绝对“更好”，取决于你想要**更硬核**还是**更温和**。

### 2. 推荐方向（在保留当前架构前提下）

- **保持当前“一次用药只加一次心情、只算一次耐受”的补丁逻辑**（已经合理）。  
- **可选增强（按需选用）：**  
  1. **过量**：在 LactatingItems.xml 的 EM_Prolactin 的 CompProperties_Drug 里增加 `overdoseSeverityOffset`、`largeOverdoseChance`，和 1.5 类似，避免无限堆药无代价。  
  2. **耐受按体型**：给 EM_Prolactin_Tolerance 的 outcomeDoer 加 `divideByBodySize>true`，体型大的人每次加的耐受少一点，更合理。  
  3. **戒断是否加强**：若希望更接近 1.5，可把戒断心情改为 -15、疼痛改为 3、意识改为 -20%，并可选加 Eating 惩罚。  
  4. **短期“high”负面**：若想要 1.5 那种“爽但有代价”，可考虑新增一个短期 Hediff（类似 Lact-XHigh），带轻微意识/操纵/移动惩罚；否则保持现在“只加心情、无负面”也可以。  

### 3. 不建议照搬 1.5 的点

- **泌乳**：1.5 用自定义 Lactating_Drug + 衰减 + toleranceChemical，和当前 EqualMilking 用的**原版 Lactating** 体系不同；若改成 1.5 那套需要大改泌乳逻辑和兼容性，不建议单纯为“吃药效果一致”而改。  
- **完全去掉 Harmony**：当前补丁已经只在一处、触发一次，逻辑清晰；若改成纯 XML 就要用“多个 outcomeDoer 里只在一个上挂自定义 doer”等方式才能保证一次心情+一次成瘾，复杂度未必更低。  

---

## 五、若只做最小改动（推荐）

在**不改变整体体验**的前提下，只做两处 XML 小增强即可：

1. **EM_Prolactin 的耐受 outcomeDoer** 加上 `divideByBodySize>true`（与 1.5 一致，更合理）。  
2. **CompProperties_Drug** 增加过量相关字段（overdoseSeverityOffset、largeOverdoseChance），数值可参考 1.5。  

这样当前版本的“吃药效果”在**次数与逻辑**上保持不变（一次用药一次心情、一次耐受），只在**合理性与安全性**上向 1.5 靠拢一点。

---

## 六、什么是「耐受联动」（toleranceChemical）

**耐受联动** = 在 outcome doer 上写 `toleranceChemical>某化学` 后，**同一次用药给出的效果会随小人当前对该化学的耐受程度而减弱**。

- **没有耐受联动**：每次吃药都按固定数值加（例如泌乳 +0.5、Joy +0.3），不管之前吃过多少、耐受多高，效果一样。
- **有耐受联动**：小人已有「催乳素耐受」时，同一次用药的**泌乳 / 兴奋 / Joy 偏移**会被游戏按耐受严重度打折，耐受越高，当次效果越弱（成瘾和耐受的叠加仍会照常进行）。

所以「与 Lact-X 化学耐受联动」= 该条效果（如泌乳、high、Joy）在 XML 里绑定了 `toleranceChemical>Lact-X`，高耐受时这次吃药带来的**正面效果会变弱**，更符合“越吃越不敏感”的设定。当前版本 EM_Prolactin 已对泌乳、EM_Prolactin_High、Joy 都加了 `toleranceChemical>EM_Prolactin_Chemical`，即已实现耐受联动。

---

## 七、四个常见问题

### 1. 为什么耐受本身不加 toleranceChemical？

**耐受**（EM_Prolactin_Tolerance）的 outcome doer **故意不加** `toleranceChemical`。

- 加 toleranceChemical 的效果是：**当前对该化学的耐受越高，这条效果越弱**。
- 耐受我们想要的是：**每次吃药都增加一点耐受**，和当前已经有多高耐受无关。
- 若给耐受也加上 toleranceChemical，高耐受时“加耐受”会被打折，越吃越加得少，和常见药物设计不一致。所以只有**泌乳、兴奋、Joy** 这三条与“药效”相关的加耐受联动，**耐受条本身**不加。

### 2. 为什么之前没加 Eating 惩罚？现在加上了吗？

- **之前**：为了和 1.5 的戒断强度区分开，戒断只做了意识 -15%、疼痛×2，没有做“进食能力”惩罚。
- **现在**：已在戒断阶段加上 **Eating -20%**（`<capacity>Eating</capacity><offset>-0.20</offset>`），和 1.5 的进食惩罚一致，戒断时吃饭更慢、更难受。

### 3. 还有什么建议？

- **needLevelOffset**：若希望成瘾后“满足一次药”能多顶一阵子需求，可在 CompProperties_Drug 里加 `needLevelOffset>1`（参考 1.5）。
- **描述/提示**：在 EM_Prolactin 的 description 里写一句“高耐受时药效会减弱”“过量使用可能导致药物过量”，方便玩家理解。
- **平衡**：若觉得成瘾/戒断太轻或太重，可微调 addictiveness、戒断 stage 的 painFactor/baseMoodEffect、Eating/Consciousness 的 offset。

### 4. 吃一次 vs 吃多次：产奶量、产奶时间、是否永久？

- **Lactating 严重度（Severity）**  
  - 每次 EM_Prolactin：+0.5（受耐受联动会略少）。  
  - 若已有 Lactating 且 Severity &lt; 1，再吃会**合并**：Severity 相加，但**上限被压在 0.9999**，不会到 1。  
  - 所以**只吃 EM_Prolactin、吃多次**：最多到 0.9999，**不会变成永久泌乳**（永久 = Severity ≥ 1）。  
  - **Lucilactin** 一次 +1.0，所以**吃一次 Lucilactin 就会永久泌乳**；之后再吃 EM_Prolactin 会在已有 ≥1 的 Severity 上继续叠（受 maxLactationStacks 上限，默认 5）。

- **产奶量（MilkAmount）**  
  - 公式：`基础产奶量 × EM_Milk_Amount_Factor`。  
  - 已改为**随 Severity 连续**：`milkAmountMultiplierPerStack^severity`。默认 1 时仍为 1；若在设置里调高“产奶量倍率”，则吃一次（0.5）略增、吃多次（0.9999）更多。

- **产奶时间（MilkGrowthTime / 产奶间隔）**  
  - 公式：`MilkGrowthTime = MilkIntervalDays / EM_Lactating_Efficiency_Factor`。  
  - 已改为**随 Severity 连续**：`lactatingEfficiencyMultiplierPerStack^severity`（默认 1.25）。**吃一次（0.5）**→ 效率约 1.12，产奶略快；**吃多次（0.9999）**→ 效率约 1.25，产奶时间更短。

- **吃饭量（食物消耗速度）**  
  - 饥饿倍率已改为**随 Severity 连续**：`hungerRateMultiplierPerStack^max(severity, 0.5)`（默认 1.31）。**吃一次（0.5）**→ 约 +14% 消耗；**吃多次（0.9999）**→ 约 +31%，吃更多。

- **总结表**

| 情况 | Lactating Severity | 产奶量 | 产奶时间/间隔 | 食物消耗 | 是否永久 |
|------|--------------------|--------|----------------|----------|-----------|
| 吃 1 次 EM_Prolactin | 0.5 | 基础×1^0.5≈1 | 间隔/1.12（略快） | 约 +14% | 否，会衰减 |
| 吃 2 次 EM_Prolactin | 0.9999 | 基础×1^0.9999≈1 | 间隔/1.25（更快） | 约 +31% | 否，会衰减 |
| 吃 1 次 Lucilactin | 1.0 | 同上档 | 更快（1.25×） | +31% | **是** |
| 已永久后再吃 EM_Prolactin | 1→2→… | 可再提高 | 更快 | 更高 | 保持永久 |

**结论**：吃一次和吃多次**有区别**：多次吃会**缩短产奶时间**、**提高食物消耗**；产奶量在设置里“产奶量倍率”&gt;1 时也会随吃药次数增加。只吃 EM_Prolactin 不会永久，要永久需至少一次 Lucilactin。
