# Equal Milking × Cumpilation 合并/迁移方案

## 一、需求重新整理

### 1.1 核心思路

- **吸奶泵 = 同一套“流体抽取”逻辑**：一台设备既可以用于**吸奶**，也可以用于**排精**（精液排出）。
- **两模组物品全部重新归类**：明确每个物品/建筑属于「可用于吸奶」「可用于排精」或「双用途」。

### 1.2 功能定义

| 功能 | 含义 | 当前实现位置 |
|------|------|--------------|
| **吸奶** | 从泌乳者身上抽取人奶，产物为 `EM_HumanMilk` | Equal Milking：`Building_Milking` + `JobDriver_EquallyMilk` + `CompEquallyMilkable.Gathered` |
| **排精** | 从 cumflation/体内精液 排出精液，产物为 `Cumpilation_Cum` | Cumpilation：① 手术 `ExtractCum`（手术台）；② 坐桶泄精 `JobDriver_DeflateBucket`（带 `Comp_DeflateBucket` 的家具） |

目标：**吸奶泵类设备也支持排精**；可选地，**桶/椅类设备也支持吸奶**（或至少统一“谁产谁可吃”规则）。

### 1.3 产源与食用规则（保持不变）

- 奶：谁产的奶 → `CompShowProducer.producer` → `allowedConsumers` 决定谁可吃。
- 精液：谁产的精液 → 同上（当前 Mod 通过 Patch 给 `Cumpilation_Cum` 加 `CompShowProducer`，在 ExtractCum/DeflateBucket 等单源产出处设 producer）。

合并/迁移后，两套流体都继续共用 `CanConsumeMilkProduct` 与 `allowedConsumers`。

---

## 二、两模组物品与用途归类

### 2.1 建筑/家具（“设备”）

| 模组 | defName | 中文名 | 当前用途 | 重新归类建议 |
|------|---------|--------|----------|--------------|
| Equal Milking | `EM_MilkingSpot` | 挤奶点 | 仅吸奶（地点） | **仅吸奶** |
| Equal Milking | `EM_MilkingPump` | 吸奶泵（手动） | 仅吸奶 | **吸奶 + 排精**（统一泵） |
| Equal Milking | `EM_MilkingElectric` | 电动吸奶泵 | 仅吸奶 | **吸奶 + 排精**（统一泵） |
| Cumpilation | `Cumpilation_CumBucket` | 精液桶 | 仅排精（收集+泄精） | **仅排精**（或可选：也支持吸奶→存奶） |
| Cumpilation | `stool_with_cumbucket` | 凳子+精液桶 | 仅排精 | **仅排精** |
| Cumpilation | `chair_with_cumbucket` | 椅子+精液桶 | 仅排精 | **仅排精** |
| Cumpilation | `autochair_with_cumtank` | 自动椅+精液罐 | 仅排精 | **仅排精** |
| Cumpilation | `Cumpilation_Advanced_Cum_Bucket` | 高级精液桶 | 仅排精+清洁 | **仅排精** |

- **可用于吸奶**：`EM_MilkingSpot`、`EM_MilkingPump`、`EM_MilkingElectric`（扩展后：泵也可排精）。
- **可用于排精**：所有 Cumpilation 桶/椅 + 扩展后的 `EM_MilkingPump` / `EM_MilkingElectric`。

### 2.2 流体/产物

| 模组 | defName | 中文名 | 用途 |
|------|---------|--------|------|
| Equal Milking | `EM_HumanMilk` | 人奶 | 吸奶产物；可食用、可做奶制品 |
| Vanilla / EM Patch | `Milk` | 奶（通用） | 可由 `EM_HumanMilk` 在灶台等制作 |
| Cumpilation | `Cumpilation_Cum` | 精液 | 排精产物；可食用、可做制品 |

### 2.3 药物/催乳（仅吸奶相关）

| 模组 | defName | 中文名 | 用途 |
|------|---------|--------|------|
| Equal Milking | `EM_Prolactin` | 催乳素 | 催乳，用于吸奶前提 |
| Equal Milking | `EM_Lucilactin` | Lucilactin | 强效催乳 |

### 2.4 配方/手术

| 模组 | defName | 中文名 | 用途 |
|------|---------|--------|------|
| Equal Milking | (Patch) | 人奶→奶 | 吸奶产物加工 |
| Cumpilation | `ExtractCum` | 手术：抽取精液 | 排精（手术台，单源→带 producer） |

### 2.5 小结表：设备 ↔ 功能

| 功能 | 可用设备（合并后） |
|------|--------------------|
| **仅吸奶** | EM_MilkingSpot |
| **吸奶 或 排精（统一泵）** | EM_MilkingPump、EM_MilkingElectric |
| **仅排精** | Cumpilation_CumBucket、stool_with_cumbucket、chair_with_cumbucket、autochair_with_cumtank、Cumpilation_Advanced_Cum_Bucket |
| **排精（手术）** | 任意 recipeUsers 为 Human 的手术台 + 配方 ExtractCum |

---

## 三、迁移/合并方案

### 方案 A：合并进当前 Mod（Equal Milking 扩展为奶+精液一体）

- **做法**：把 Cumpilation 的 Def + 精液相关 C#（Leaking、Gathering 等）迁入 3266052474，命名空间改为本 Mod，About 只保留一个 Mod。
- **优点**：一个 Mod 内统一“泵”逻辑、产源规则、语言；吸奶泵可直接支持排精。
- **缺点**：体量大、需跟进 Cumpilation 后续更新、与 rjw-genes 的 loadAfter 关系要改为对本 Mod。

**适用**：你希望只维护一个 Mod，且可以接受把 Cumpilation 代码都迁进来时。

---

### 方案 B：不合并模组，只做“吸奶泵可排精”+ 产源兼容（推荐）

- **做法**：
  1. **保持 Cumpilation 独立**；当前 Mod 继续用 Patch 给 `Cumpilation_Cum` 加 `CompShowProducer`，在 ExtractCum / DeflateBucket 等处设 producer（与现有文档一致）。
  2. **在当前 Mod 内**：
     - 为 **吸奶泵**（`EM_MilkingPump`、`EM_MilkingElectric`）增加“排精”能力：
       - 让吸奶泵建筑**可选挂载** Cumpilation 的 `Comp_DeflateBucket`（或本 Mod 仿写一个“排精”Comp，逻辑与 DeflateBucket 一致），使小人可以**到吸奶泵上做排精 Job**（与坐桶泄精同逻辑），产物为 `Cumpilation_Cum` 并写入 producer。
     - 或：新增 Job「到吸奶泵排精」，目标建筑为 `Building_Milking`，仅当 Cumpilation 存在且 pawn 有 cumflation 时可用；执行逻辑复用 Cumpilation 的泄精/产精。
  3. **物品归类**：仅通过 Def 与代码标注“该建筑支持吸奶 / 排精”，不移动 Cumpilation 的 Def。
- **优点**：不动 Cumpilation 本体、rjw-genes 仍 loadAfter Cumpilation；实现量集中在当前 Mod。
- **缺点**：两 Mod 并存，需保证加载顺序（当前 Mod loadAfter Cumpilation）。

**实现要点（方案 B）**：
- 在 3266052474 中新增可选 Patch：当检测到 Cumpilation 存在时，给 `EM_MilkingPump` / `EM_MilkingElectric` 添加 `CompProperties_DeflateBucket`（或等价 Comp），使 `ThingDefsDeflate.bucketDefs` 自动包含两台泵（因为 Cumpilation 用 `HasComp<Comp_DeflateBucket>` 收集“可排精”建筑）。
- 或：本 Mod 自建“可排精建筑”列表，新增 JobGiver「去吸奶泵排精」；JobDriver 复用 Cumpilation 的泄精与产精逻辑（通过 Harmony 或反射调用），产出的精液再打上 producer。

---

### 方案 C：新建“流体抽取”抽象层（大重构）

- **做法**：引入抽象概念「流体抽取设备」：
  - 定义 `FluidType`：Milk / Cum（可扩展）。
  - 建筑 Def 上标注 `supportedFluids: [Milk, Cum]`；Job/WorkGiver 根据 pawn 状态（泌乳 / cumflation）与建筑支持的流体类型，决定是吸奶还是排精。
  - 吸奶泵、桶/椅都挂到同一套“流体抽取”逻辑下，再按 supportedFluids 区分。
- **优点**：扩展性最好，后续加其它流体也方便。
- **缺点**：要动两边的 Job/WorkGiver/Comp，工作量大；且 Cumpilation 不合并的话，抽象层要跨 Mod，复杂度高。

**适用**：长期规划、愿意做大重构时。

---

## 四、推荐路线与步骤（方案 B）

1. **需求与归类**  
   - 以本文第二节为准，在语言/说明里标明：吸奶泵也可用于排精（当 Cumpilation 存在时）。

2. **实现“吸奶泵可排精”**（二选一或组合）  
   - **2a**：XML Patch 在 Cumpilation 存在时，给 `EM_MilkingPump`、`EM_MilkingElectric` 添加 `CompProperties_DeflateBucket`，使 Cumpilation 的 `JobGiver_Deflate` 把吸奶泵视为可排精建筑；并在本 Mod 的 Harmony 里，对“从吸奶泵格上生成精液”的路径设 `CumProducerForNextSpawn = pawn`，保证产源正确。  
   - **2b**：本 Mod 单独写 Job + JobDriver「到吸奶泵排精」，仅当 Cumpilation 存在且地图有吸奶泵时可用，逻辑上调用 Cumpilation 的泄精与产精（或复制其算法），产出精液带 producer。

3. **产源与食用**  
   - 保持现有：`Cumpilation_Cum` 已带 `CompShowProducer`，ExtractCum/DeflateBucket 单源处设 producer；吸奶泵排精的新路径同样设 producer。  
   - 食用侧继续用 `CanConsumeMilkProduct`，无需改。

4. **桶/椅是否支持吸奶**  
   - 可选：若希望“凳子+桶”也能吸奶，需要 Cumpilation 侧或本 Mod 为桶/椅增加“接受奶”的存储与 Job（例如允许 `EM_HumanMilk` 放入桶），并接上 `CompEquallyMilkable.Gathered` 的吸奶逻辑。工作量较大，建议作为后续扩展，不在首版合并方案中必须实现。

5. **文档与版本**  
   - 在 About、说明或加载顺序说明中写清：本 Mod 在 Cumpilation 存在时，吸奶泵可用于排精；精液与奶共用“谁产谁可吃”规则。

---

## 五、总结表

| 项目 | 建议 |
|------|------|
| 模组是否合并 | **不合并**；当前 Mod 做“吸奶泵可排精”+ 产源兼容（方案 B） |
| 吸奶泵 | 扩展为 **吸奶 + 排精**（当 Cumpilation 存在时） |
| 桶/椅 | 保持 **仅排精**；是否支持吸奶作为可选后续 |
| 物品归类 | 按第二节表格：泵=双用途，桶/椅=仅排精，Spot=仅吸奶 |
| 产源与食用 | 奶与精液共用 `CompShowProducer` + `CanConsumeMilkProduct` |

实现完成后，加载顺序：**Equal Milking loadAfter Cumpilation**，以便 Patch 与 Comp 正确挂到 Cumpilation 的 Def 与生成逻辑上；吸奶泵上的排精产出需在本 Mod 的 Harmony 中统一打上 producer。

---

## 六、操作与产出逻辑整理

### 6.1 玩家可配置的操作（按性别）

| 对象 | 操作项 | 说明 |
|------|--------|------|
| **女性** | 是否允许在**手动挤奶点**挤奶 | 对应挤奶 Job 的可用条件（如 `allowMilking`） |
| **女性** | 是否允许在**吸奶泵/电动泵**上挤奶 | 同上，设备为 EM_MilkingPump / EM_MilkingElectric |
| **女性** | 指定 **1. 谁可以吸我的奶** | `CompEquallyMilkable.allowedSucklers`；空=默认（子女+伴侣等） |
| **女性** | 指定 **2. 谁可以食用我产的奶制品** | `CompEquallyMilkable.allowedConsumers`；空=仅自己 |
| **男性** | 是否允许**手动排精**（坐桶/凳/椅泄精） | 对应 DeflateBucket 类 Job 的可用条件（可选实现） |
| **男性** | 是否允许在**吸奶泵/机器**上排精 | 方案 B 扩展后，泵也可排精时的权限控制 |
| **男性** | **1. 伴侣性交排精** | 即 Cumflation/Gathering；产主=射精者，由 sources 记录 |
| **男性** | 指定 **2. 谁可以食用我的精液制品** | 与奶共用 `allowedConsumers`（产主名单） |
| **男性** | 是否保留**手术排精**（ExtractCum） | 可选：设为不可用或从菜单隐藏，则仅保留“坐桶/泵排精”与“性交进桶” |

### 6.2 产出逻辑：药物

| 对象 | 药物 | 作用 |
|------|------|------|
| **女性** | 催乳药 **EM_Prolactin** / **EM_Lucilactin** | 给 Lactating hediff，进入/加强泌乳期，从而产奶；Lucilactin 易成瘾可维持长期泌乳 |
| **男性** | **Cumpilation_Lecithin**（卵磷脂） | 给 Cumpilation_ActiveCowpersGland，**partFluidMultiplier ×1.5**，增加单次精液量 |
| 说明 | rjw-genes 壮阳/aphrodisiac 类 | 只加 **SexFrequency**（更常想做爱），**不**增加单次精液量 |

### 6.3 产出逻辑：自然产出

| 对象 | 方式 | 说明 |
|------|------|------|
| **女性** | **进入泌乳期后产奶** | ① **吃药**：Prolactin/Lucilactin → Lactating；② **产后**：动物分娩时本 mod 给 Lactating（人类产后是否给 Lactating 由原版/其它 mod 决定）；③ **基因** EM_Permanent_Lactation；④ 设置「成年雌性动物始终泌乳」 |
| **女性** | 奶的积累 | **持续、被动**：Lactating 的 Charge 随营养/时间增加，再通过挤奶 Job 变成 EM_HumanMilk |
| **男性** | **性交产出精液** | **事件驱动**：无“身体自然攒精液”；仅通过性行为（RJW TransferFluids）产生 FluidAmount → Cumflation 或直接进桶（Gathering），再经手术/坐桶泄精/清洁变成 Cumpilation_Cum |
| **男性** | 精液来源 | 射精者生殖器 hediff 的 FluidAmount（单次射精量）；卵磷脂/基因可提高该量 |

---

## 七、已定与待实现要点

**Mod 定位**：本方案为**新 Mod**，名称**「精液和母乳」**（或副标题形式）；基于 Equal Milking + Cumpilation 整合。后续可扩展**妹汁**等其它流体。

以下为第七章结论与实现约定。

### 7.1 双性 / 扶她（Futa）——已定

- **两个 Job 都可选**：同一小人既泌乳又有 cumflation 时，挤奶 Job 与排精 Job **都可接**，不二选一。
- **三个独立配置项**：
  1. **谁可吸奶**（allowedSucklers）
  2. **谁可以使用我的奶制品**（奶的 allowedConsumers）
  3. **谁可以使用我的精液制品**（精液的 allowedConsumers，与奶共用同一套产主名单时可合并为“谁可使用我产的奶/精液制品”）

### 7.2 吸奶泵双用途——已定

- **吸奶泵吸取精液与母乳的逻辑一致**：排精与挤奶在泵上采用同一套“流体抽取→落格、打产主”的逻辑；产物（EM_HumanMilk / Cumpilation_Cum）**直接落泵格**，不进入泵的“库存”与管道（见 7.6）。

### 7.3 Job 优先级与需求——建议

- **需求驱动**：同一小人既可挤奶又可排精时，建议**谁更满/更急就先做哪个**——例如奶满（Charge 高）优先接挤奶、cumflation 满优先接排精；若都满则按 WorkGiver 列表顺序。
- 实现方式：在挤奶 / 排精 Job 的**可用条件**里加“另一项未满或优先级更低时才接”的权重或条件（可选），避免两个 Job 同时抢人。

### 7.4 囚犯 / 奴隶——已定

- **囚犯/奴隶产奶或产精液时，产主默认仅自己**：allowedConsumers 为空即“仅产主本人可食用”；殖民者若未在产主名单中则不可食用其奶/精液制品。不单独做“囚犯产主默认允许殖民者”的开关。

### 7.5 心情 / 记忆 / 社交——已定

- **被挤奶 vs 被泄精**：**共用“是否自愿”的框架**——若产主已允许（allowedSucklers / allowedConsumers 包含对方），记为自愿，心情中性或轻微正面；若强制（未允许却被使用），可给负面记忆。
- **他人食用自己产出的制品**：可选给轻微正面记忆（例如伴侣吃自己产的奶/精液制品）。**EM_HadSexWhileLactating** 已有，可保留。

### 7.6 吸奶泵与管道——已定

- **吸奶泵产出的母乳（EM_HumanMilk）和精液（Cumpilation_Cum）均不进入管道**，直接落格。
- **只有动物产出的原版奶（Milk）才进入管道**（与现有 Equal Milking PipeSystem 行为一致）。人奶与精液不参与管道网。

### 7.7 清洁 / 污物——已定

- **精液污物→清洁→产出物品**：地图上的**精液污物**（filth）被小人**清洁**时，按 **FluidGatheringDef**（canBeRetrievedFromFilth、filthNecessaryForOneUnit）在清洁完成后生成 **Cumpilation_Cum** 物品（“从地上精液回收成瓶装精液”）。合并后该逻辑保留在本 Mod 内。
- **母乳污物→清洁→产出物品**：**同样实现**——做**奶污物 Def**（人奶泼洒在地的污物），清洁时按与精液类似的规则产出 **EM_HumanMilk**（“从地上奶污物回收成瓶装人奶”）。即母乳与精液都走「污物→清洁→产出物品」这一套。

### 7.8 人类产后泌乳——原理、效果与比方

- **“给 Lactating”是什么**：游戏里有一个叫 **Lactating** 的**状态**（hediff），谁身上带着这个状态，谁就**处于泌乳期**，可以产奶、被挤奶、喂奶。**“给 Lactating”= 给小人加上这个泌乳状态**，相当于让 TA 进入“可以产奶”的模式。
- **原理**：原版 RimWorld（Biotech）定义了 **Lactating** 这个状态名；本 Mod 把它的**内部逻辑**换成了自己的（产奶量、谁可吸奶等）。人类妈妈**生完孩子**后，若**没有**这个状态，就不会产奶、也不能喂奶；若**在分娩结束时给她加上 Lactating**，她就会像动物产后一样自动进入泌乳期。
- **效果**：若妈妈产后被加上了 Lactating，则**自动可以喂奶、可以被挤奶**，不需要再吃催乳药。上述效果的前提是本 Mod 实现“人类分娩结束时自动给妈妈加 Lactating”（否则需吃催乳药或依赖原版/其它 mod）。
- **打个比方**：就像“产后自动点亮「可以产奶」这个技能”——没点时妈妈没有奶，点后就可以产奶、挤奶、被吸奶。本 Mod 可选实现“人类分娩结束时自动给妈妈加 Lactating”（与动物产后加 Lactating 对称）。

### 7.9 药物分工——已定（合并后仅本 Mod）

- **Cumpilation 合并进本 Mod 后，只保留本 Mod**，不再单独装 Cumpilation；**Galactogogues**（增产药）也随合并进入本 Mod。
- **分工写清**：在 Mod 说明里写清——**进入泌乳期**用 **EM_Prolactin / EM_Lucilactin**；**已有泌乳时提高产奶量**用 **Galactogogues**。两类药都在本 Mod 内，可并存使用。
- 可选：在设置页加一句“催乳进泌乳期用 Prolactin/Lucilactin，增产用 Galactogogues”的提示。

### 7.10 rjw-genes 特殊基因与产源——已定

- 若基因把“胸”产出改为精液（如 **cum milk**），**产主仍是该小人**；allowedConsumers / CompShowProducer 沿用同一套，谁产谁可吃。

### 7.11 存档与 Mod 定位——已定

- **新 Mod「精液和母乳」**：本方案按**新 Mod** 发布，名称/副标题为“精液和母乳”；**对旧存档做兼容/修复**（例如从旧 Equal Milking 或 Cumpilation 存档加载时，补上产主、allowedConsumers 等缺失数据，避免报错或食用规则错乱）。
- **后续扩展**：往后可添加**妹汁**等其它流体，沿用同一套“产主 + 谁可食用”规则。

### 7.12 本地化与说明

- 新增/修改的键名（如“谁可食用我的精液制品”、“谁可吸奶”等）需**多语言**；复用 Cumpilation 键名时避免覆盖导致语义变化。

### 7.13 Cumpilation 合并时尚未完全纳入或需单独约定的功能

以下为 Cumpilation 原有功能，合并进「精液和母乳」后需逐项决定：是否保留、是否套用“产主 + 谁可食用”规则、或仅保留逻辑不接产主。

| 功能 | 说明 | 约定 |
|------|------|--------|
| **Cumflation 过满喷出**（OverflowingCumflation） | 体内精液 severity>1 时自动接 Job 喷精到周围 | **与 sources 一致**：喷出的精液/污物打产主 |
| **Stuffing（Cumstuffed）** | 灌肠/填塞，与 Cumflation 不同的 hediff，可排出 | **走 allowedConsumers**：排出时产主记录，食用受产主名单限制 |
| **Bukkake** | 体表/颜射污物（覆盖在身上） | **带产主**：清洁自己后回收的物品打产主（通常为射精者） |
| **Leaking**（HediffComp_LeakCum） | 自然泄漏成地面污物（不进桶） | **视为混合**：清洁回收后的精液不追溯产主，任何人可吃 |
| **DeflateClean** | 泄精到地上成污物（而非进桶） | **带产主**：落地与清洁回收均打产主 |
| **VomitFluid** | 呕吐精液（随机吐精） | **带产主**：吐出的物品产主=该小人 |
| **Plug / Seal**（Comp_SealCum、ApparelSealCum） | 塞子/密封防止泄漏 | 与产主无关，合并后保留 |
| **CleanSelf / WorkGiver_CleanSelf** | 清洁自己身上的 bukkake 等 | 保留；清洁产生的物品产主见 Bukkake |
| **Oscillation** | 高潮时生成 hediff 或改 severity | 与产主无关，保留 |
| **Biosculptor 周期** | 生物塑形舱：增加精液量、重置精液量 | 合并后在本 mod 内，说明/本地化即可 |
| **Slug（Biotech）** | 蛞蝓流体、SlugStuffed、死亡爆炸等 | **套用**：给对应 ThingDef 加 CompShowProducer 与产主，谁产谁可吃 |
| **Thoughts / Records** | 消费精液记录、阶段想法（吃多了等） | **可选**：区分“食用自己/伴侣”的额外心情 |
| **Trait Likes_Cumflation** | 喜欢被灌满 | 保留 |
| **ThingFilter / 食物限制**（Cum_Food_Restrictions） | 食物策略里可限制精液 | 与 allowedConsumers 并行：策略管“能不能选精液”，产主管“选了谁的精液可吃” |
| **Not-Semen-Processor**（子 mod） | 精液加工成**建材与织物**：Samenstein（精液→石料类建材）、Spermatuch（精液→织物）；配方 Make_Samenstein（12 精液→8）、Make_Spermatuch（15 精液→6） | **纳入合并**；制品是否继承原料产主（CompShowProducer）待后续约定 |
| **Ideology / DBH / 其它 DLC Patch** | 依赖 DLC 的补丁 | 合并后保留，按原 Cumpilation 条件加载 |

### 7.14 迁移前最终检查与 Not-Semen-Processor 约定

- **遗漏检查**：第七章 7.1–7.13 已覆盖产主规则、泵/桶、污物清洁、Slug、Thoughts、rjw-genes、存档兼容、Not-Semen-Processor 等；合并后 rjw-genes 的 loadBefore 改为对本 Mod（akaster.equalmilking / 精液和母乳），不再依赖 vegapnk.cumpilation。
- **Not-Semen-Processor**：**纳入合并**；Samenstein / Spermatuch 制品是否继承原料产主（CompShowProducer）**待后续约定**，首版合并仅保留功能与配方，产主逻辑可后续补丁。
- 上述确认后执行迁移：将 Cumpilation 的 Defs、Patches、Mods、Common（贴图等）、Source、Languages 并入本 Mod，更新 About / LoadFolders，移除「需 Cumpilation 激活」的判断，最后删除独立 cumpilation 目录。

