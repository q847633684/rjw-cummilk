# 游戏中药品（ThingDef）变量参考

便于查阅：本 mod 与 RimWorld 原版中，**药品 ThingDef** 及其 **ingestible / outcomeDoers / comps** 里常见、可配置的变量。实际字段以游戏/原版 Def 为准，此处仅作清单与示例。

---

## 一、ThingDef 通用（药品继承）

| 变量 | 类型/示例 | 说明 |
|------|-----------|------|
| defName | string | 定义名，唯一 |
| label | string | 显示名 |
| description | string | 描述 |
| techLevel | TechLevel | 科技等级 |
| statBases | 若干 StatModifier | MarketValue, Mass, WorkToMake 等 |
| graphicData | GraphicData | 贴图、drawSize、color |
| costList | 资源列表 | 制造消耗 |
| recipeMaker | 制造相关 | recipeUsers, workSpeedStat, bulkRecipeCount 等 |

---

## 二、ingestible（服用相关）

药品通常继承带 `<ingestible>` 的父类（如 DrugBase / EM_DrugBase / MakeableDrugBase）。

| 变量 | 本 mod 示例 | 说明 |
|------|-------------|------|
| drugCategory | Medical | 药品分类 |
| foodType | Processed, Fluid | 食物类型 |
| baseIngestTicks | 90 / 300 / 400 | 服用耗时（tick） |
| preferability | NeverForNutrition | 是否当食物 |
| ingestCommandString | Inject {0} | 命令文案 |
| ingestReportString | Injecting {0}. | 报告文案 |
| ingestSound | Ingest_Inject | 服用音效 |
| nurseable | true/false | 是否可喂给婴儿 |
| **outcomeDoers** | 见下表 | 服用后执行的效果列表 |

### outcomeDoers 里常用子节点

每个 outcomeDoer 有 `Class`（类型），类型不同则子变量不同。

#### IngestionOutcomeDoer_GiveHediff（给 hediff）

| 变量 | 本 mod 示例 | 说明 |
|------|-------------|------|
| hediffDef | Lactating, EM_Prolactin_Tolerance, Cumpilation_ActiveMammaries | 要添加的 HediffDef |
| severity | 0.5, 2, 0.044, 0.176, 1.0 | 严重度（若可合并则与已有累加） |
| toleranceChemical | EM_Prolactin_Chemical | 关联的耐受化学物；高耐时原版会削弱本次 severity |
| divideByBodySize | true | 是否按体型分摊（如耐受 severity） |

#### IngestionOutcomeDoer_OffsetNeed（需求偏移）

| 变量 | 本 mod 示例 | 说明 |
|------|-------------|------|
| need | Joy | 需求类型 |
| offset | 0.3, 1.2 | 偏移量 |
| toleranceChemical | EM_Prolactin_Chemical | 可选，耐受联动 |

其他常见类型（原版/其他 mod）：IngestionOutcomeDoer_AddHediff、IngestionOutcomeDoer_IngestDrug 等，各有自己的字段。

---

## 三、comps（CompProperties_Drug 等）

药品常用 `CompProperties_Drug` 控制成瘾与过量。

| 变量 | 本 mod 示例 | 说明 |
|------|-------------|------|
| chemical | EM_Prolactin_Chemical | 化学物 defName，用于耐受/成瘾 |
| addictiveness | 0, 0.04, 1 | 成瘾度 |
| minToleranceToAddict | 0, 0.03 | 耐受达到多少才开始成瘾 |
| existingAddictionSeverityOffset | 0.2, 1 | 已有成瘾时再吃的严重度偏移 |
| needLevelOffset | 1 | 需求等级偏移（成瘾需求） |
| overdoseSeverityOffset | min/max | 过量时增加的伤害严重度 |
| largeOverdoseChance | 0.01 | 大过量概率 |
| isCombatEnhancingDrug | false | 是否战斗增强药 |
| listOrder | 1001 | UI 排序 |

---

## 四、本 mod 三种药品用到的变量汇总

### EM_Prolactin（LactatingItems.xml）

- **ingestible.outcomeDoers**  
  - GiveHediff(Lactating)：severity **0.5**，toleranceChemical EM_Prolactin_Chemical  
  - GiveHediff(EM_Prolactin_Tolerance)：severity **0.044**，divideByBodySize true  
  - OffsetNeed(Joy)：offset 0.3，toleranceChemical EM_Prolactin_Chemical  
- **comps.CompProperties_Drug**：chemical, addictiveness 0.04, minToleranceToAddict 0.03, existingAddictionSeverityOffset 0.2, overdoseSeverityOffset, largeOverdoseChance  
- **baseIngestTicks**：90  

### EM_Lucilactin（LactatingItems.xml）

- **ingestible.outcomeDoers**  
  - GiveHediff(Lactating)：severity **2**，toleranceChemical EM_Prolactin_Chemical  
  - GiveHediff(EM_Prolactin_Tolerance)：severity **0.176**，divideByBodySize true  
  - OffsetNeed(Joy)：offset 1.2，toleranceChemical EM_Prolactin_Chemical  
- **comps.CompProperties_Drug**：chemical, addictiveness 1, minToleranceToAddict 0, existingAddictionSeverityOffset 1, needLevelOffset 1, overdoseSeverityOffset, largeOverdoseChance  
- **baseIngestTicks**：90  

### Cumpilation_Galactogogues（Drug_FluidBuff.xml）

- **ingestible.outcomeDoers**  
  - GiveHediff(Cumpilation_ActiveMammaries)：severity **1.0**（无 toleranceChemical）  
- **comps.CompProperties_Drug**：addictiveness 0, overdoseSeverityOffset, largeOverdoseChance  
- **baseIngestTicks**：300  

---

## 五、与「生效天数」相关的变量

- **当前代码没有**在药品 Def 里定义「生效天数」或「持续天数」。  
- 生效时长由 **Hediff 的 severity + 该 Hediff 的每日变化** 决定：  
  - Lactating：severity 由 outcomeDoer 的 **severity** 累加，每日由 `HediffComp_EqualMilkingLactating.SeverityChangePerDay()` 扣减 **-0.1×(1+耐受)**。  
  - Cumpilation_ActiveMammaries：severity 1.0，HediffDef 里 **severityPerDay -0.33**，约 3 天消退。  
- 若以后要在**药品**上做「生效天数」配置，可以：  
  - 在 outcomeDoer 上扩展自定义字段（如 `effectDays`），或  
  - 继续用「给的 hediff + 该 hediff 的 severityPerDay」间接表示天数。

---

*文件位置：`Docs/药品Def变量参考.md`。*
