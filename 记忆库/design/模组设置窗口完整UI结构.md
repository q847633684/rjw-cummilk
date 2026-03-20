# 模组设置窗口完整 UI 结构

本文档描述 Equal Milking 模组设置窗口的完整层级与内容（专业级 7 主 Tab 方案），便于对照代码与模拟显示。

---

## 窗口布局示意图（ASCII）

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  [窗口标题] Equal Milking / Mod settings                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  [主 Tab]  核心机制 │ 健康风险 │ 权限规则 │ 数值平衡 │ 模组联动 │ 数据种族 │ [调试工具] │
├─────────────────────────────────────────────────────────────────────────────┤
│  [子 Tab]  （随主 Tab 变化，见下表）                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─ 内容区（可滚动）─────────────────────────────────────────────────────┐   │
│  │  灰字说明 (SectionDesc_*)                                              │   │
│  │  □ 复选框 / 滑块 / 文本框 …                                            │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **调试工具** 仅在 `Prefs.DevMode` 为真时显示。
- 主 Tab 切换时 `subTabIndex` 置 0；若 `subTabIndex >= subTabs.Count` 会钳位为 0。

---

## 窗口整体

- **入口**：`MilkCumMod.DoSettingsWindowContents` → `MilkCumSettings.DoWindowContents(inRect)`。
- **布局**：顶部主 Tab 栏 → 子 Tab 栏（有则显示）→ 内容区（带内边距，可滚动）。

---

## 第一层：主 Tab 栏（7 个）

| 索引 | 枚举 | Keyed | 中文 |
|------|------|-------|------|
| 0 | CoreSystems | EM.Tab.CoreSystems | 核心机制 |
| 1 | HealthRisk | EM.Tab.HealthRisk | 健康风险 |
| 2 | Permissions | EM.Tab.Permissions | 权限规则 |
| 3 | Balance | EM.Tab.Balance | 数值平衡 |
| 4 | Integrations | EM.Tab.Integrations | 模组联动 |
| 5 | DataRaces | EM.Tab.DataRaces | 数据种族 |
| 6 | DevTools | EM.Tab.DevTools | 调试工具（仅 DevMode） |

---

## 第二层：子 Tab（随主 Tab 变化）

### 主 Tab「核心机制」(CoreSystems)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.BreastfeedSystem | Widget_BreastfeedSettings.DrawBreastfeedSystemFull（哺乳系统：总览+营养+人形/动物/机械） |
| 1 | EM.SubTab.CumSystem | Widget_CumpilationSettings（精液 Cumpilation） |
| 2 | EM.SubTab.FluidBehavior | 占位灰字：EM.SubTab.FluidsGirlJuicePlaceholder（妹汁未实现） |

### 主 Tab「健康风险」(HealthRisk)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.MastitisSystem | Widget_AdvancedSettings.DrawHealthSection(0)：乳腺炎 |
| 1 | EM.SubTab.HygieneSystem | DrawHealthSection(1)：DBH 卫生 |
| 2 | EM.SubTab.ToleranceSystem | DrawHealthSection(2)：耐受 |
| 3 | EM.SubTab.OverflowPollution | DrawHealthSection(3)：溢出与污染 |

### 主 Tab「权限规则」(Permissions)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.MenuVisibility | Widget_AdvancedSettings：身份与菜单（催乳素/挤奶菜单显示） |
| 1 | EM.SubTab.DefaultBehavior | Widget_DefaultSetting：按身份默认（自我挤奶、可被喂奶等） |

### 主 Tab「数值平衡」(Balance)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.BalanceScaling | Widget_AdvancedSettings.DrawSection(Balance, 0)：乳房与池（容量、流速、剩余天数、泌乳增益） |

### 主 Tab「模组联动」(Integrations)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.RJWIntegration | DrawIntegrationSectionExtended(0)：RJW 联动 |
| 1 | EM.SubTab.DBHIntegration | DrawIntegrationSectionExtended(1)：DBH（同健康风险中的 DBH 设置来源） |
| 2 | EM.SubTab.NutritionSystem | DrawNutritionBlock：营养→能量、泌乳额外饥饿、回缩吸收（EM.SectionDesc_NutritionIntegration） |

### 主 Tab「数据种族」(DataRaces)

| subTabIndex | Keyed | 内容 |
|-------------|-------|------|
| 0 | EM.SubTab.Milk | Widget_MilkableTable（产奶表） |
| 1 | EM.SubTab.MilkTags | Widget_MilkTagsTable（奶标签） |
| 2 | EM.SubTab.RaceOverrides | Widget_RaceOverrides（种族覆盖） |
| 3 | EM.SubTab.GenesAndAdvanced | Widget_GeneSetting + DevMode 时 advancedSettings.DrawDevModeSection |

### 主 Tab「调试工具」(DevTools)

- **无子 Tab**。内容区：`Widget_AdvancedSettings.DrawDevModeSection(contentRect)`（选中小人泌乳调试、L/池信息、LactationPoolTickLog 等）。

---

## 内容区控件与区块摘要

| 控件 / 区块 | 主要选项或内容 |
|-------------|----------------|
| **DrawBreastfeedSystemFull** | 哺乳总览说明、营养块（能量/额外饥饿/回缩）；人形/动物/机械三类哺乳设置合并一页。 |
| **Widget_CumpilationSettings** | Cumpilation 精液/体液开关与参数（膨胀、填充、覆盖、收集等）。 |
| **DrawHealthSection(0) 乳腺炎** | 允许乳腺炎、基准 MTB、满池/卫生风险系数、人形/动物 MTB 乘数。 |
| **DrawHealthSection(1) DBH** | 乳腺炎卫生用 DBH 还是房间清洁度（仅当 DBH 激活时显示）。 |
| **DrawHealthSection(2) 耐受** | 耐受影响产奶、指数等。 |
| **DrawHealthSection(3) 溢出与污染** | 溢出污物 Def、AI 优先挤更满、池模型参考说明。 |
| **身份与菜单** | 生产者限制提示、药物/身份提示；菜单显示：机械/殖民者/奴隶/囚犯/动物/其它。 |
| **Widget_DefaultSetting** | 按身份（殖民者/奴隶/囚犯/动物/机械/实体）的「允许自我挤奶」「可被喂奶」默认。 |
| **Balance 乳房与池** | 泌乳期增大 RJW 乳房、容量系数、流速倍率、基准/分娩泌乳天数、泌乳增益开关与百分比。 |
| **RJW / DBH / 营养** | 联动 → DrawIntegrationSectionExtended / DrawNutritionBlock。 |
| **DrawDevModeSection** | DevMode：选中小人泌乳调试、L/池信息、LactationPoolTickLog 勾选。 |

---

## 与代码的对应

- 主 Tab 列表：`MilkCumSettings.cs` 中 `mainTabs`（6 个 + 条件 DevTools），点击时 `subTabIndex = 0`。
- 子 Tab 列表：`GetSubTabs()` 按 `mainTabIndex` 返回；`subTabIndex` 钳位见 `DoWindowContents`。
- 内容分发：`DoWindowContents` 的 `switch (mainTabIndex)` 与各 case 内 `subTabIndex` 分支。
- 健康/权限/平衡/联动绘制：`Widget_AdvancedSettings.DrawSection(mainTab, subTab)`，内部按 `mainTab` 与 `subTab` 调用 `DrawHealthSection`、`DrawIntegrationSectionExtended`、`DrawNutritionBlock` 等。

---

*用于模拟显示、文档对照与 UI 重构参考。专业级方案见 记忆库/design/模组设置UI专业级重构方案.md。*
