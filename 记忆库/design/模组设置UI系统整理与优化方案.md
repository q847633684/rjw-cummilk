# 模组设置 UI 系统整理与优化方案

对照总览中的「UI 系统：模组设置 UI（基因/种族设置 + 精液/母乳/妹汁 3 个子设置）」与当前代码实现，做一次系统性整理，并给出可选的优化方案（不改变行为的前提下提升可维护性与可读性）。

**方案 B 已实施**：主 Tab 已重组为「精液 / 母乳 / 基因与种族 / 权限与默认」四栏；母乳下合并哺乳、健康与风险、效率与界面、RJW 为 **8** 个子 Tab（人形/动物/机械合并为「哺乳（按类型）」一页）；精液下含妹汁占位；基因与种族集中为奶表/奶标签/种族覆盖/基因；权限与默认为身份与菜单 + 按身份默认。详见下文「一、当前结构」表（已按方案 B 更新）。

---

## 一、当前结构（方案 B 实施后）

### 1.1 入口与主 Tab

- **入口**：`MilkCumMod.DoSettingsWindowContents` → `MilkCumSettings.DoWindowContents(inRect)`。
- **主 Tab（4 个，方案 B）**：由 `MainTabIndex` 枚举与 `mainTabs` 列表定义：

| 主 Tab 索引 | 枚举名 | 文案 Keyed | 含义 |
|-------------|--------|------------|------|
| 0 | Cum | EM.Tab.Cum | 精液 |
| 1 | Lactation | EM.Tab.Lactation | 母乳 |
| 2 | GenesAndRaces | EM.Tab.GenesAndRaces | 基因与种族 |
| 3 | PermissionsAndDefaults | EM.Tab.PermissionsAndDefaults | 权限与默认 |

### 1.2 子 Tab 与内容分发

| 主 Tab | 子 Tab（subTabIndex） | 内容控件 | 说明 |
|--------|------------------------|----------|------|
| **精液** | 0 精液 / 1 妹汁 | Widget_CumpilationSettings / 占位灰字 | Cumpilation 设置；妹汁待实现 |
| **母乳** | 0 哺乳总览 / 1 哺乳（人形/动物/机械）/ 2 乳腺炎 / 3 DBH / 4 耐受与溢出 / 5 身份与菜单 / 6 乳池 / 7 RJW | Widget_BreastfeedSettings（0 总览、1 DrawAllTabs）；Widget_AdvancedSettings.DrawSection(Lactation, 2–7) | 哺乳、健康、效率、RJW 合并为同一主 Tab 下 8 个子 Tab |
| **基因与种族** | 0 产奶表 / 1 奶标签 / 2 种族覆盖 / 3 基因与高级 | Widget_MilkableTable / Widget_MilkTagsTable / Widget_RaceOverrides / Widget_GeneSetting + DevMode | 奶类型表、标签、种族覆盖、基因配置 |
| **权限与默认** | 无子 Tab | Widget_AdvancedSettings.DrawSection(PermissionsAndDefaults, 0) + Widget_DefaultSetting | 身份与菜单（催乳素等可见性）+ 按身份默认（可挤奶/可被喂） |

### 1.3 与总览「模组设置 UI」的对应关系

方案 B 实施后已对齐总览：

- **基因/种族设置**：主 Tab「基因与种族」下 4 个子 Tab（产奶表、奶标签、种族覆盖、基因与高级）。
- **精液**：主 Tab「精液」，子 Tab 0 为 Cumpilation 设置。
- **母乳**：主 Tab「母乳」，子 Tab 0–7 覆盖哺乳、健康、效率、RJW。
- **妹汁**：主 Tab「精液」下子 Tab 1 为占位（灰字「尚未实现」）。
- **权限设置**：主 Tab「权限与默认」，无子 Tab，内容为身份与菜单 + 按身份默认。

---

## 二、问题与可优化点（整理）

### 2.1 结构层面

| 问题 | 说明 |
|------|------|
| 主 Tab 与总览「3 个子设置」不对齐 | 总览为「精液、母乳、妹汁」三个子设置，当前为 5 个主 Tab（产奶与体液、哺乳、健康与风险、效率与界面、联动与扩展），新人或文档读者易困惑。 |
| 基因/种族分散 | 种族覆盖在「产奶与体液」，基因在「联动与扩展」，若希望「基因/种族设置」作为一块入口，需跨两个主 Tab。 |
| 母乳相关设置分散 | 乳腺炎/耐受在「健康与风险」，乳池/身份/默认在「效率与界面」，哺乳在「哺乳」；逻辑上都属「母乳」系统内开关，但入口分散。 |
| 妹汁无占位 | 若将来做妹汁，需新增主 Tab 或子 Tab，当前无预留。 |

### 2.2 代码与可维护性

| 问题 | 说明 |
|------|------|
| Widget_AdvancedSettings 职责过重 | 健康、效率、联动三个主 Tab 的多个子 Tab 内容均由同一 Widget 的 DrawSection + 多个 Draw*Block 完成，单文件约 370 行，新增/修改选项时易碰同一文件。 |
| 子 Tab 索引与 MainTab 耦合 | GetSubTabs() 与 DoWindowContents 的 switch 中大量 mainTabIndex + subTabIndex 分支，新增 Tab 需改多处。 |
| 精液文案 Keyed 不统一 | Widget_CumpilationSettings 使用 `cumpilation_*`、`cumpilation_cumsettings_*` 等 Keyed，与母乳侧 `EM.*` 命名风格不同，翻译与检索不统一。 |
| 固定滚动高度 | Widget_AdvancedSettings 使用 SectionScrollContentHeight = 2400f，极端分辨率或内容增多时可能截断或留白过多；见 列表滚动与固定contentHeight。 |

### 2.3 已做得好的点（保持）

- 主 Tab 已用 `MainTabIndex` 枚举，子 Tab 在 Widget_AdvancedSettings 内用命名常量（SubTabHealth_* 等）。
- 母乳相关区块普遍使用 EM.* + Tooltip 回退；Window_ProducerRestrictions、Dialog_GeneConfig 等已有 EM 标题。
- 效率与界面 subTab 0 上半区（身份与菜单）+ 下半区（按身份默认）分区清晰，Widget_DefaultSetting 独立。

---

## 三、优化方案（可选，按优先级）

### 方案 A：仅文档与注释（零代码结构变更）

- **做法**：在记忆库/总览/索引中明确写清「当前主 Tab 为功能维度（产奶、哺乳、健康、效率、联动），与总览中的体液维度（精液/母乳/妹汁）为两种视角；设置项与总览节点对应关系见本表」；在 MilkCumSettings 或 Widget_AdvancedSettings 顶部注释中增加「主 Tab 对应关系」表。
- **效果**：阅读代码或文档时能快速对应「要找母乳的乳腺炎 → 健康与风险 → 乳腺炎」。
- **代码量**：仅增注释与 1 份小表（如放在 design/模组设置UI系统整理与优化方案.md 或 系统结构总览 第四节下）。

### 方案 B：主 Tab 重组为「精液 / 母乳 / 妹汁 / 基因与种族 / 权限与默认」（大改）

- **做法**：MainTabIndex 改为例如 Fluids（精液+妹汁占位）、Lactation（母乳：合并哺乳、健康、效率中与母乳相关的子 Tab）、GenesAndRaces（基因+种族覆盖）、PermissionsAndDefaults（按身份默认、菜单显示等）。每个主 Tab 下再分子 Tab。
- **效果**：与总览「3 个子设置 + 基因/种族」一致，找「母乳」或「精液」时只需进一个主 Tab。
- **代价**：DoWindowContents、GetSubTabs、Widget_AdvancedSettings.DrawSection 及所有 mainTabIndex/subTabIndex 分支均需重写；翻译 Keyed 的 Tab 名需增或改；存档中若持久化 mainTabIndex 需做兼容或放弃记忆上次 Tab。
- **建议**：仅当确实希望「按体液分主 Tab」且愿意接受一次大改时再做；否则方案 A 即可。

### 方案 C：Widget 拆分（中改，不换 Tab 结构）

- **做法**：将 Widget_AdvancedSettings 按区块拆成多个小 Widget，例如：Widget_HealthSettings（乳腺炎、DBH、耐受溢出）、Widget_EfficiencySettings（身份与菜单、乳池）、Widget_IntegrationSettings（RJW、DBH）。MilkCumSettings 仍按现有 mainTab/subTab 调用，但改为调用对应 Widget 的 Draw，DrawSection 只做简单转发。
- **效果**：单文件行数下降，修改「仅乳腺炎」时只需改 Widget_HealthSettings；与「母乳/精液」的文档对应关系可在各 Widget 注释中写清。
- **代价**：需从 MilkCumSettings 或 AdvancedSettings 传入当前所需的 subTab 或区块枚举；DevMode 区块保留在某一 Widget 或单独小块。

### 方案 D：精液 Keyed 与母乳统一（小改）

- **做法**：为 Cumpilation/Leak 相关文案增加 EM.Cumpilation.* 或 EM.Fluids.* 的 Keyed，在 Widget_CumpilationSettings 中改用 EM.*，原 cumpilation_* 保留为 fallback 或逐步迁移；或约定「精液沿用 cumpilation_*，仅文档注明」。
- **效果**：翻译与检索风格统一，便于与 EM 词条一起维护。
- **代价**：需在 LanguageData 中增键或做键映射；若 Cumpilation 为上游/共享键则需协商。

### 方案 E：基因与种族入口聚合（中改）

- **做法**：在「产奶与体液」主 Tab 下将「种族覆盖」与「基因」放在相邻子 Tab（例如 2=种族覆盖、3=基因），并把「联动与扩展」下的「基因与高级」改为仅「RJW / DBH / DevMode」，不再在联动里重复基因；或增加一个主 Tab「基因与种族」仅包含两个子 Tab（基因、种族覆盖），原两处内容改为复用同一 Widget。
- **效果**：符合总览「基因/种族设置」一块入口，减少「基因在哪」的困惑。
- **代价**：需调整 GetSubTabs 与 DoWindowContents 分支，以及子 Tab 文案。

---

## 四、推荐组合与顺序

1. **先做方案 A**：文档与注释补全，零风险，立即改善「总览与代码对应」的可读性。
2. **视需要做方案 D**：若希望翻译与 Keyed 统一，再统一精液侧 Keyed（或明确约定保留 cumpilation_*）。
3. **视需要做方案 C**：若后续继续在设置页加选项，建议拆 Widget，避免 Widget_AdvancedSettings 继续膨胀。
4. **方案 B（主 Tab 重组）**：仅在有明确需求「必须按体液分主 Tab」时再做，并单独排期。
5. **方案 E**：若希望「基因/种族」单入口，可在方案 C 或 B 时一并考虑（例如新 Tab「基因与种族」或子 Tab 顺序调整）。

---

## 五、与系统结构总览的同步

- 总览第四节「UI 系统」中已写「模组设置 UI：基因/种族设置 + 精液、母乳、妹汁 3 个子设置」；本节说明当前实现为「5 主 Tab（功能维度）」，与总览为两种视角，对应关系见上表。
- 若采纳方案 B 或 E 并改 Tab 结构，需同步更新：记忆库/design/系统结构总览.md 第四节、记忆库/design/UI审阅结论.md 主 Tab 列表、Source/索引.md 中设置 UI 相关行。

---

## 六、小结

| 项目 | 内容 |
|------|------|
| 当前主 Tab | 产奶与体液 / 哺乳 / 健康与风险 / 效率与界面 / 联动与扩展（5 个，功能维度） |
| 与总览差异 | 总览为「基因/种族 + 精液/母乳/妹汁」；当前未按体液分主 Tab，基因与种族分散在两处 |
| 建议优先 | 方案 A（文档与注释）；可选方案 D（Keyed 统一）、方案 C（Widget 拆分） |
| 大改可选 | 方案 B（主 Tab 按体液重组）、方案 E（基因/种族入口聚合） |
