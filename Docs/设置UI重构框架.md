# Equal Milking 设置 UI 重构框架

## 一、现状问题

1. **分类混乱**：「高级」页塞入泌乳效率、哺乳时间、界面显示、RJW/DBH 联动、乳腺炎、溢出、种族覆盖、从 Def 加载等，主题不统一。
2. **归属不清**：如「哺乳时间」在高级里，与「哺乳」Tab 分开；种族白名单/黑名单在高级里，与「种族/挤奶」Tab 分离。
3. **缺少说明**：部分选项仅有 key 无描述（Tooltip），或描述过于简略，用户不清楚用途与推荐值。
4. **Tab 命名不直观**：如「重命名/奶类型」实际是物品标签（种族/显示），「默认」实际是「按身份默认允许挤奶/被喂奶」。
5. **产奶与体液分离**：精液/体液(Cumpilation)已并入本 mod、与产奶共用产主与食用规则，但设置里仍单独占一个 Tab，未与产奶合并展示，不利于「体液类产出」统一管理。

---

## 二、重构目标

- **按「功能域」分 Tab**：采用 **主 Tab 栏 + 子 Tab 栏** 两层结构；主 Tab 为少数大类，子 Tab 为具体区块，避免单层 7 个 Tab 过平。
- **产奶与体液合并**：产奶系统和精液/体液系统(Cumpilation)机制高度重合，在主 Tab「产奶与体液」下用子 Tab（产奶 | 奶标签 | 种族覆盖 | 精液/体液）统一管理。
- **每项必有**：短标签（名称）+ 可选的一行描述（Tooltip）；重要数值带推荐范围或说明。
- **联动按 mod 分组**：主 Tab「联动与扩展」下子 Tab 为 RJW、DBH、基因与高级，并注明「仅当安装 XXX 时生效」。

---

## 三、主 Tab + 子 Tab 结构

采用 **主 Tab 栏**（顶层分类）+ **子 Tab 栏**（当前主 Tab 下的分页），切换主 Tab 时子 Tab 列表随之变化。

### 主 Tab 1：产奶与体液

| 子 Tab | 内容 | 说明/描述需求 |
|--------|------|----------------|
| **产奶** | Widget_MilkableTable（种族→是否挤奶、奶类型、奶量） | 表头或顶部加一句「指定每种种族产何种奶、是否可挤奶」。 |
| **奶标签** | Widget_MilkTagsTable（物品→标种族/标小人） | 区块标题与一句「奶制品在信息中是否显示种族/产奶者」。 |
| **种族覆盖** | 白名单、黑名单、人形流速倍率 defaultFlowMultiplierForHumanlike | 每项 Tooltip：白名单=强制可泌乳，黑名单=强制不可，流速=与人形产奶速度平衡。 |
| **精液/体液** | Widget_CumpilationSettings 全部（膨胀、填充、覆盖、收集、泄精等） | 说明：与产奶共用产主与食用规则，此处调节膨胀/填充/泄精等行为。 |

### 主 Tab 2：哺乳

| 子 Tab | 内容 | 说明/描述需求 |
|--------|------|----------------|
| **总览** | 哺乳总览说明、营养→能量、哺乳时间 breastfeedTime | 谁可以喂谁、单次哺乳时长、机械族营养换算。 |
| **人形** | Widget_BreastfeedSettings 人形部分 | 允许哺乳、可喂人/动物/机械等。 |
| **动物** | Widget_BreastfeedSettings 动物部分 | 同上。 |
| **机械族** | Widget_BreastfeedSettings 机械族部分 | 同上。 |

### 主 Tab 3：健康与风险

| 子 Tab | 内容 | 说明/描述需求 |
|--------|------|----------------|
| **乳腺炎** | allowMastitis、MTB、满池/卫生风险系数、人形/动物 MTB 乘数 | 每项 Tooltip。 |
| **卫生(DBH)** | useDubsBadHygieneForMastitis（仅当 DBH 激活时显示） | 用 DBH 卫生需求 vs 房间清洁度。 |
| **耐受与溢出** | allowToleranceAffectMilk、toleranceFlowImpactExponent；overflowFilthDefName、aiPreferHighFullnessTargets、只读参考天数 | 耐受影响产奶、溢出污物 Def、AI 优先挤更满。 |
| **从 Def 加载** | 按钮 ApplyDefaultsFromDef | 从 EM_Defaults Def 应用默认值，不覆盖种族列表。 |

### 主 Tab 4：效率与界面

| 子 Tab | 内容 | 说明/描述需求 |
|--------|------|----------------|
| **泌乳效率** | lactatingEfficiencyMultiplierPerStack、lactatingGainEnabled、lactatingGainCapModPercent、femaleAnimalAdultAlwaysLactating | 泌乳期效率倍率、意识/操纵/移动增益、动物默认泌乳。 |
| **身份与菜单** | Widget_DefaultSetting；showMechOptions 等催乳素菜单显示；ProducerRestrictionsHint、DrugRoleHint | 按身份默认挤奶/被喂奶、菜单显示哪些身份、生产者/药物提示。 |

### 主 Tab 5：联动与扩展

| 子 Tab | 内容 | 说明/描述需求 |
|--------|------|----------------|
| **RJW** | 仅当 RJW 激活时显示：rjwBreastSizeEnabled、rjwLustFromNursingEnabled 等 | 区块下注明「需要 RJW 模组」；每项补全 Desc。 |
| **DBH** | useDubsBadHygieneForMastitis（仅当 DBH 激活时显示） | 描述已有。 |
| **基因与高级** | Widget_GeneSetting；DevMode 时选中小人显示泌乳状态等 | 基因决定的奶类型覆盖；开发模式调试信息。 |

---

## 三 A、原单层 Tab 细项参考（已映射到主/子 Tab）

以下为原「单层 7 Tab」的细项列表，保留作实现时对照。

### 原 Tab 1～7 细项

### Tab 1：产奶与体液（合并：原种族/产奶 + 奶标签 + 种族覆盖 + Cumpilation）

产奶与精液/体液在机制上高度重合（产主、食用规则、收集、溢出等），放在同一 Tab 便于统一管理。

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **产奶** | 现有 Widget_MilkableTable（种族→是否挤奶、奶类型、奶量） | 保留；表头或顶部加一句「指定每种种族产何种奶、是否可挤奶」。 |
| **奶物品标签** | 现有 Widget_MilkTagsTable（物品→标种族/标小人） | 保留；加区块标题与一句「奶制品在信息中是否显示种族/产奶者」。 |
| **种族覆盖** | 白名单 raceCanAlwaysLactate、黑名单 raceCannotLactate、人形流速倍率 defaultFlowMultiplierForHumanlike | 从高级移入；每项需 Tooltip：白名单=强制可泌乳，黑名单=强制不可，流速=与人形产奶速度平衡。 |
| **精液/体液 (Cumpilation)** | 现有 Widget_CumpilationSettings 全部内容（膨胀、填充、覆盖、收集、泄精/Leaking 等） | 与产奶同页；区块标题「精液与体液」并加一句说明：与产奶共用产主与食用规则，此处调节膨胀/填充/泄精等行为。 |

### Tab 2：哺乳（原 Tab 3 + 高级里哺乳时间等）

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **哺乳总览** | 一句说明：谁可以喂谁、在下面分类型设置。 | 新增 EM.BreastfeedOverviewDesc。 |
| **营养→能量** | nutritionToEnergyFactor（机械族哺乳） | 保留；Tooltip 说明用于机械族哺乳时的营养换算。 |
| **哺乳时间** | breastfeedTime | 从高级移入；Tooltip：单次哺乳动作时长（tick）。 |
| **人形/动物/机械族** | 现有 Widget_BreastfeedSettings 三子 Tab | 保留；每类「允许哺乳」「可喂人/动物/机械」等已有逻辑，补一句区块描述即可。 |

### Tab 3：健康与风险（乳腺炎、耐受、溢出、AI）

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **从 Def 加载默认** | 按钮 ApplyDefaultsFromDef | 保留；描述写清：从 EM_Defaults Def 应用关键默认值，不覆盖种族列表；其他 mod 可 patch 该 Def。 |
| **乳腺炎** | allowMastitis、mastitisBaseMtbDays、overFullnessRiskMultiplier、hygieneRiskMultiplier、人形/动物 MTB 乘数 | 保留；每项 Tooltip：是否启用、基准 MTB（天）、满池过久风险系数、卫生风险系数、人形/动物 MTB 倍率。 |
| **卫生来源** | useDubsBadHygieneForMastitis（仅当 DBH 激活时显示） | 保留；描述：用 DBH 卫生需求 vs 房间清洁度。 |
| **耐受** | allowToleranceAffectMilk、toleranceFlowImpactExponent | 保留；描述：催乳素耐受是否影响产奶效率、指数曲线。 |
| **溢出与污物** | overflowFilthDefName、baselineMilkDurationDays/birthInducedMilkDurationDays（只读参考）、aiPreferHighFullnessTargets | 保留；描述：满池溢出地面污物 Def、参考天数说明、殖民者是否优先挤更满的目标。 |

### Tab 4：泌乳效率与增益（从高级拆出）

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **泌乳效率** | lactatingEfficiencyMultiplierPerStack | 保留；描述：泌乳期意识/产奶效率的全局倍率。 |
| **泌乳期增益** | lactatingGainEnabled、lactatingGainCapModPercent | 保留；描述：是否启用意识/操纵/移动增益、增益上限百分比。 |
| **动物默认泌乳** | femaleAnimalAdultAlwaysLactating | 保留；描述：成年雌性动物是否默认处于泌乳状态（便于产奶）。 |

### Tab 5：界面与默认行为（原「默认」+ 高级里界面显示）

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **按身份默认** | 现有 Widget_DefaultSetting：殖民者/奴隶/囚犯/动物/机械/实体 → allowMilkingSelf、canBeFed | 保留；区块标题改为「按身份默认：允许自我挤奶 / 可被喂奶」；canBeFed 描述写清「是否允许被喂奶（奶制品）」。 |
| **催乳素菜单显示** | showMechOptions、showColonistOptions、showSlaveOptions、showPrisonerOptions、showAnimalOptions、showMiscOptions | 保留；区块标题「在催乳素/泌乳相关菜单中显示哪些身份」；每项 Tooltip：勾选后该身份会在挤奶/泌乳界面中显示。 |
| **生产者/药物提示** | 仅保留说明文字 ProducerRestrictionsHint、DrugRoleHint（不移动逻辑，只作为本页顶部说明） | 可选：移到「种族与产奶」Tab 顶部或保留在「高级」底部。 |

### Tab 6：模组联动（仅 RJW、DBH）

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **RJW 联动** | 仅当 rim.job.world 激活时显示：rjwBreastSizeEnabled、rjwLustFromNursingEnabled、rjwSexNeedLactatingBonusEnabled、rjwSexSatisfactionAfterNursingEnabled、rjwLactationFertilityFactor、rjwLactatingInSexDescriptionEnabled、rjwSexAddsLactationBoost、rjwSexLactationBoostDeltaS | 每项已有或补全 Desc；区块标题下注明「需要 RJW 模组」。 |
| **Dubs Bad Hygiene** | useDubsBadHygieneForMastitis | 仅当 DBH 激活时显示；描述已有；可在此集中一处，或保留在「健康与风险」中仅 DBH 区块。 |

说明：精液/体液(Cumpilation)已与本 mod 合并，设置放在 Tab 1「产奶与体液」，不在此联动页。

### Tab 7：基因与高级

| 区块 | 内容 | 说明/描述需求 |
|------|------|----------------|
| **奶类型基因** | 现有 Widget_GeneSetting | 保留；标题「基因决定的奶类型覆盖」。 |
| **开发模式** | 选中小人显示泌乳状态 L/双池/E/卫生风险等 | 仅 Prefs.DevMode 时显示；可保留在最后一页。 |

---

## 四、主 Tab 与子 Tab 顺序建议

| 主 Tab 序号 | 主 Tab 名称（Key） | 子 Tab 列表 |
|-------------|---------------------|-------------|
| 1 | EM.Tab.MilkAndFluids | 产奶 \| 奶标签 \| 种族覆盖 \| 精液/体液 |
| 2 | EM.Tab.Breastfeed | 总览 \| 人形 \| 动物 \| 机械族 |
| 3 | EM.Tab.HealthAndRisk | 乳腺炎 \| 卫生(DBH) \| 耐受与溢出 \| 从 Def 加载 |
| 4 | EM.Tab.EfficiencyAndInterface | 泌乳效率 \| 身份与菜单 |
| 5 | EM.Tab.IntegrationAndAdvanced | RJW \| DBH \| 基因与高级 |

若希望减少主 Tab 数量，可将主 Tab 4 与 3 部分合并（效率并入健康与风险下子 Tab），或保持 5 个主 Tab 以保持清晰。

---

## 五、语言与描述规范

- **每个设置项**：至少一条 `EM.XXX`（标签）和一条 `EM.XXXDesc`（Tooltip）；若为通用概念可复用 Lang. 或现有 key。
- **每个区块**：一条 `EM.Section.XXX` 作为区块标题（灰色小标题）。
- **联动区块**：标题旁注明 `(需要 XXX 模组)` 或 `(Requires XXX)`，由 key 控制。
- **数值**：Slider 的 label 可含当前值；Tooltip 中写推荐范围或「越大/越小则…」。

---

## 六、实现步骤建议

1. **Phase 1**：在 `EqualMilkingSettings.cs` 中实现 **主 Tab 栏**（5 个主 Tab）与 **子 Tab 栏**（根据当前 mainTabIndex 显示对应子 Tab 列表）；contentRect 根据 mainTabIndex + subTabIndex 分发到现有 Widget；同步增加/调整 Lang key（EM.Tab.* 主 Tab，EM.SubTab.* 子 Tab）。
2. **Phase 2**：拆 Widget_AdvancedSettings，按主/子 Tab 表迁入对应 Widget。**主 Tab 1** 下子 Tab 分别对应产奶表、奶标签、种族覆盖、Widget_CumpilationSettings。
3. **Phase 3**：补全所有缺失的 `*Desc` Tooltip，并统一区块标题与灰色说明文字。
4. **Phase 4**：整理 Languages 下 Keyed/DefInjected，确保中英 key 一致、无空描述。

---

## 七、文件与引用关系（重构后）

- **EqualMilkingSettings.cs**：DoWindowContents 绘制 **主 Tab 栏**（如 TabRecord 或自定义按钮）、**子 Tab 栏**（主 Tab 切换时更新子 Tab 列表）、contentRect 根据 mainTabIndex + subTabIndex 调用对应 Widget。
- **Widget_***.cs**：每个（主 Tab, 子 Tab）组合对应一个 Widget 或同一 Widget 内分支；主 Tab 1 下 4 个子 Tab 分别对应产奶表、奶标签、种族覆盖、Cumpilation。
- **Languages/**：Keyed 中 EM.Section.*、EM.Tab.*、各选项的 Desc；DefInjected 若有设置相关注入可保持。

此框架可直接用于后续按 Phase 逐步重构与补描述；若需要先实现某一 Phase 的代码草稿，可指定 Phase 与 Tab 编号。
