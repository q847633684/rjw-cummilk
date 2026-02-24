# Cumpilation / RJW-Genes 与当前 Mod 的整合说明

## 1. Cumpilation 是否可以合并到当前 Mod？

**结论：可以但不建议。**

- **当前 Mod**（3266052474 = Equal Milking, `akaster.equalmilking`）：奶、泌乳、产源限制（谁产的奶谁能吃/谁可吸）。
- **Cumpilation**（`vegapnk.cumpilation`）：RJW 流体（精液、cumflation、桶、收集等），体量大、作者与 packageId 不同，且 **rjw-genes 依赖 Cumpilation**（loadAfter）。
- **合并**：技术上可以把 Cumpilation 代码拷进本 Mod 并改 About/命名空间，但会变成“奶 + 精液”一体 Mod，维护和版本升级都会变复杂。

**推荐做法**：**不合并**，保持 Cumpilation 独立；在**当前 Mod 里做“可选兼容”**：当检测到 Cumpilation 存在时，对精液物品应用与奶相同的**产源限制**（谁产的精液/精液制品，只有产主允许的名单可以食用/使用）。

---

## 2. 精液与奶同规则：谁产的精液 → 谁可以吃制品和吸食

需求：和奶一样——**谁产的精液，就由谁决定谁可以食用精液/精液制品**（含“吸食”等使用方式）。

### 当前 Mod 已有机制（奶）

- 奶物品带 `CompShowProducer`（`producer` = 产奶者）。
- 食用前：`WorkGiver_Ingest`、`JobDriver_Ingest` 用 `CanConsumeMilkProduct(pawn, thing)` 过滤。
- `CanConsumeMilkProduct`：无 producer 则允许；否则看产奶者 `CompEquallyMilkable().allowedConsumers`，空=仅自己，非空=自己+列表。

### 实现思路（精液，不合并 Cumpilation）

在**当前 Mod** 中做“可选 Cumpilation 兼容”：

1. **精液物品也带产源**
   - 用 **XML Patch** 给 Cumpilation 的 `Cumpilation_Cum` 增加 `CompProperties_ShowProducer`（与奶共用同一 Comp）。
   - 在**有明确产源**时给精液设 `producer`：
     - **Recipe_ExtractCum**：从某小人身上抽取 → 产源 = 该小人（`pawn`）。
     - **从桶放精（JobDriver_DeflateBucket）**：桶内 hediff 的 `HediffComp_SourceStorage.sources` 按 (pawn, fluid, amount) 记录；放精时 Cumpilation 按权重选一个 `FluidSource` 决定本次放出的 fluid。本 Mod 在 `SpawnCum(fluid)` 的 Prefix 里从同一 hediff 的 sources 中筛出该 fluid 的条目、按 amount 加权随机一个产主，设为 `CumProducerForNextSpawn`，这样本批次放出的精液都会带上该产主标签。
     - 多源混合且无法区分时（其他路径）：不设 producer，与“无 producer 的奶”一致。

2. **食用/使用限制**  
   - 精液（及用精液做的制品）若带 `CompShowProducer.producer`，**直接复用**现有逻辑：
     - `CanConsumeMilkProduct(consumer, thing)` 已对任意带 `CompShowProducer` 的 Thing 做检查。
     - 产主用 `CompEquallyMilkable().allowedConsumers`（当前 Mod 的 `CompEquallyMilkable()` 会在需要时给 pawn 动态挂上 comp），故**精液产主也共用同一套“谁可以吃我产的东西”名单**。
   - 无需改 Cumpilation 或 rjw-genes 的食用逻辑，只要本 Mod 的 `WorkGiver_Ingest` / `JobDriver_Ingest` 补丁生效即可（已对任意食物做 `CanConsumeMilkProduct`）。

3. **“吸食”**  
   - 若“吸食”指食用精液物品，则同上，被 `JobDriver_Ingest` + `CanConsumeMilkProduct` 覆盖。
   - 若有单独的“吸食”Job/Verb，需要在该 Job/Verb 的可行或执行处增加对 `thing.TryGetComp<CompShowProducer>()` 与 `consumer.CanConsumeMilkProduct(thing)` 的校验（与奶的用法一致）。

### 需要改动的点（在当前 Mod）

- **About.xml**：增加对 `vegapnk.cumpilation` 的 **loadAfter**（可选依赖，不写 modDependencies 即可不强制订阅）。
- **Patch XML**：给 `ThingDef Cumpilation_Cum` 添加 `comps` → `CompProperties_ShowProducer`。
- **Harmony**：
  - 对 `Cumpilation.Leaking.Recipe_ExtractCum` 的 `SpawnCum`：Prefix 设 `CumProducerForNextSpawn = pawn`，Postfix 清空；`ThingMaker.MakeThing` Postfix 对生成的 `Cumpilation_Cum` 写入 `CompShowProducer.producer`。
  - 对 `Cumpilation.Leaking.JobDriver_DeflateBucket` 的 `SpawnCum`：Prefix 从当前 cumflationHediff 的 `HediffComp_SourceStorage.sources` 中按当前 fluid 过滤、按 amount 加权选一个产主并设 `CumProducerForNextSpawn`，Postfix 清空；同一 `ThingMaker.MakeThing` Postfix 负责给本批次放出的精液打上产主。
  - 若存在其他单源产出路径且能拿到产主 pawn，可同样在生成 Thing 前设 `CumProducerForNextSpawn`。
- **ExtensionHelper**：`CanConsumeMilkProduct` 已通用，无需改；若后续有“仅奶”的逻辑，需保证精液带 `CompShowProducer` 时也被当作“有产主的产品”处理（当前实现已是按 comp 判断，无需改）。

---

## 3. rjw-genes 也一样

**结论：无需合并 rjw-genes，也无需改 rjw-genes 源码。**

- rjw-genes 在 Cumpilation 之后加载，使用同一套精液物品（如 `Cumpilation_Cum`）和食用/配方逻辑。
- 在当前 Mod 中为精液挂上 `CompShowProducer` 并在生成时写入 `producer` 后：
  - 任何人食用精液或精液制品时，都会经过本 Mod 的 `WorkGiver_Ingest` / `JobDriver_Ingest` 和 `CanConsumeMilkProduct`。
  - 因此 **rjw-genes 下的精液消费/制品** 会自动遵守“谁产的精液谁能吃”的规则，无需在 rjw-genes 里再实现一遍。

若 rjw-genes 中有**非进食的**“使用精液”路径（例如某能力、某 Job 直接消耗精液），且希望也受产源限制，则需在本 Mod 中对该 Job/能力做一次 Harmony 补丁，在“使用/消耗”前调用与 `CanConsumeMilkProduct` 等价的检查（或直接复用该方法）。

---

## 4. 小结

| 项目 | 建议 |
|------|------|
| Cumpilation 合并进当前 Mod | 不合并；当前 Mod 做可选兼容即可。 |
| 精液“谁产谁可吃/吸食” | 在当前 Mod 中：给 `Cumpilation_Cum` 加 CompShowProducer；在 ExtractCum 等单源产出处设 producer；复用 CanConsumeMilkProduct 与 allowedConsumers。 |
| rjw-genes | 保持独立；精液食用自动遵守上述规则；若有非进食的消耗路径再单独补丁。 |

实现完成后，加载顺序建议：**Equal Milking (当前 Mod) loadAfter Cumpilation**，以便 Patch 和 Comp 正确挂到 Cumpilation 的 Def 与生成逻辑上。
