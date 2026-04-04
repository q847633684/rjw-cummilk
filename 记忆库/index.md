# 记忆索引

按类型与标签聚合的记忆条目目录。新增条目后请在本页对应类型下补充链接与简短说明。

---

## docs（规格文档，由原 Docs/ 迁移）

| 标题 | 路径 | 说明 |
|------|------|------|
| 规格文档索引 | [docs/README](docs/README.md) | 泌乳逻辑图、参数表、清单、模拟与脚本的入口 |
| 泌乳系统逻辑图 | [docs/泌乳系统逻辑图](docs/泌乳系统逻辑图.md) | 端到端流程、公式与常数、双池/进水/挤奶（权威规格） |
| 参数联动表 | [docs/参数联动表](docs/参数联动表.md) | 设置参数—影响逻辑—建议联动 |
| 游戏已接管变量与机制清单 | [docs/游戏已接管变量与机制清单](docs/游戏已接管变量与机制清单.md) | 原版/RJW 已接管项，本 mod 只读不重写 |
| 药品Def变量参考 | [docs/药品Def变量参考](docs/药品Def变量参考.md) | 药品 ThingDef/ingestible 与本 mod 三种药品 |
| RJW 乳房定义参数与使用说明 | [docs/RJW乳房定义与rjw-cummilk使用说明](docs/RJW乳房定义与rjw-cummilk使用说明.md) | RJW 乳房 Def/API 参数与本 mod 的读写边界、代码映射 |
| 耐受系统重构设计 | [docs/耐受系统重构设计](docs/耐受系统重构设计.md) | E_tol、进水/衰减公式、落地修改清单 |
| 与游戏本体及RJW的冲突与优化总结 | [docs/与游戏本体及RJW的冲突与优化总结](docs/与游戏本体及RJW的冲突与优化总结.md) | 冲突与优化 |
| 与游戏机制对齐清单（泌乳与药品） | [docs/与游戏机制对齐清单-泌乳与药品](docs/与游戏机制对齐清单-泌乳与药品.md) | 已用/未用原版机制对照（SeverityPerDay、剩余时间、药品配置），对齐 RJW Humpshroom 建议 |
| UI 文案前后对照 | [docs/UI文案前后对照](docs/UI文案前后对照.md) | 术语统一、健康页悬停、硬编码冒号、改文建议与实施顺序 |
| Integrations 索引 | [docs/Integrations索引](docs/Integrations索引.md) | RJW、PipeSystem、VME、Dubs 路径/命名空间/Harmony ID 收口 |
| Harmony Patch 主题清单 | [docs/HarmonyPatch主题清单](docs/HarmonyPatch主题清单.md) | 主 mod / RJW / VME / Pipe 的 Patch 按主题分类与注册顺序 |
| 其它 | 见 [docs/README](docs/README.md) | 待办、建议评估、模拟、设置UI、脚本、报错记录、Integrations 等 |

---

## 记忆库根目录

| 标题 | 路径 | 说明 |
|------|------|------|
| 记忆库说明与检索入口 | [README](README.md) | 用途、目录结构、使用方式与维护约定 |
| 长记忆模型架构说明 | [架构说明](架构说明.md) | 记忆层级 L2/L3/L4、类型与 schema、写入/检索约定 |
| 记忆库建议与改进 | [建议与改进](建议与改进.md) | 维护保鲜、覆盖补全、检索与结构改进建议 |
| 记忆库变更记录 | [changelog-memory](changelog-memory.md) | 记忆库本身的新增、废弃与重要修订 |

---

## design（设计决策）

| 标题 | 路径 | 摘要/标签 |
|------|------|-----------|
| 设计决策概览 | [design/00-概览](design/00-概览.md) | 设计决策概览与撰写指引 |
| 泌乳逻辑前提：仅在有乳房时进行 | [design/泌乳前提-仅在有乳房时](design/泌乳前提-仅在有乳房时.md) | #双池 #泌乳前提 #乳房 #GetBreastPoolEntries |
| 双池结构与 PairIndex、选侧规则 | [design/双池与PairIndex](design/双池与PairIndex.md) | #双池 #PairIndex #选侧 #GetBreastPoolEntries #DrainForConsume |
| 泌乳刷新策略与性能 | [design/泌乳刷新策略与性能](design/泌乳刷新策略与性能.md) | #tick #性能 #CompTick #LOD |
| 泌乳与胀满因果顺序 | [design/泌乳与胀满因果顺序](design/泌乳与胀满因果顺序.md) | #双池 #胀满 #泌乳 #逻辑 |
| 快满时：停产、回缩、溢出与补营养 | [design/快满-停产-回缩-溢出](design/快满-停产-回缩-溢出.md) | #双池 #回缩 #溢出 #压力 |
| 压力因子满池乘数与溢出模拟 | [design/压力因子满池乘数与溢出](design/压力因子满池乘数与溢出.md) | #双池 #压力 #溢出 |
| 架构原则与重组建议 | [design/架构原则与重组建议](design/架构原则与重组建议.md) | #架构 #SOLID #重组 #命名空间 #Integrations |
| Source 目录结构梳理 | [design/Source目录结构梳理](design/Source目录结构梳理.md) | #目录 #索引 #层级 |
| 未使用代码与函数清单 | [design/未使用代码与函数清单](design/未使用代码与函数清单.md) | #未使用 #DeadCode #API |
| 逻辑审阅：营养与泌乳一致性 | [design/逻辑审阅-营养与泌乳一致性](design/逻辑审阅-营养与泌乳一致性.md) | #审阅 #营养 #乳池 #1:1 #Need_Food |
| 吸奶时间模拟 | [design/吸奶时间模拟](design/吸奶时间模拟.md) | #吸奶 #时间 #公式 #模拟 |
| 挤奶时间模拟 | [design/挤奶时间模拟](design/挤奶时间模拟.md) | #挤奶 #时间 #公式 #模拟 #流速 |
| UI 审阅结论 | [design/UI审阅结论](design/UI审阅结论.md) | #UI #审阅 #设置 #翻译 |
| 激素模型：催乳素维持与泌乳抑制 | [design/激素模型-催乳素与排乳反馈](design/激素模型-催乳素与排乳反馈.md) | #激素 #催乳素 #排乳反馈 #泌乳抑制 |
| 排乳反馈与原有系统不兼容说明 | [design/排乳反馈与原有系统不兼容说明](design/排乳反馈与原有系统不兼容说明.md) | #排乳反馈 #兼容 #原有系统 #L衰减 |
| 乳汁系统内子系统划分与目录建议 | [design/乳汁系统内子系统划分与目录建议](design/乳汁系统内子系统划分与目录建议.md) | #乳汁 #泌乳 #池系统 #挤奶吸奶 #目录 |
| 系统结构总览 | [design/系统结构总览](design/系统结构总览.md) | #系统结构 #母乳 #精液 #妹汁 #UI #建筑物品 |
| 列表滚动与固定 contentHeight | [design/列表滚动与固定contentHeight](design/列表滚动与固定contentHeight.md) | #UI #滚动 #BeginScrollView #contentHeight |
| 代码审阅：泌乳系统核心逻辑 | [design/代码审阅-泌乳系统核心逻辑](design/代码审阅-泌乳系统核心逻辑.md) | #审阅 #泌乳 #双池 #UpdateMilkPools #CompEquallyMilkable |
| 代码审阅：药物系统实现确认 | [design/代码审阅-药物系统实现确认](design/代码审阅-药物系统实现确认.md) | #审阅 #药物 #吸收延迟 #耐受公式 #WorldComponent_MilkCumAbsorptionDelay |
| 奶与奶制品权限开放接口 | [design/奶与奶制品权限开放接口](design/奶与奶制品权限开放接口.md) | #权限 #开放接口 #AllowSucklerHandlers #CanConsumeMilkProductHandlers #扩展 |
| 拟真子系统开关清单与优先级 | [design/拟真子系统开关清单与优先级](design/拟真子系统开关清单与优先级.md) | #拟真 #顺应性 #代谢门控 #漏奶 #RJW形变 #优先级 #P0P1P2 |

---

## conventions（项目约定）

| 标题 | 路径 | 摘要/标签 |
|------|------|-----------|
| 约定概览 | [conventions/00-概览](conventions/00-概览.md) | 约定概览与与 skills 的对应 |
| EMDefOf 与核心代码位置 | [conventions/EMDefOf与代码位置](conventions/EMDefOf与代码位置.md) | #EMDefOf #命名空间 #代码结构 |

---

## domain（领域概念）

| 标题 | 路径 | 摘要/标签 |
|------|------|-----------|
| 领域术语概览 | [domain/00-概览](domain/00-概览.md) | 领域术语概览与 Docs 引用 |
| Mod 定位：流体系统与三大系统 | [domain/mod定位-流体系统三大系统](domain/mod定位-流体系统三大系统.md) | #流体系统 #精液 #乳汁 #妹汁 #一系统一目录 |
| 代码常量与公式对应 | [domain/代码常量与公式对应](domain/代码常量与公式对应.md) | #常量 #公式 #PoolModelConstants #tick |
| 快速参考（参数联动、兼容、坑点） | [domain/快速参考](domain/快速参考.md) | #参数 #兼容 #边界 #坑点 |

---

## decisions（关键决策）

| 标题 | 路径 | 摘要/标签 |
|------|------|-----------|
| 关键决策概览 | [decisions/00-概览](decisions/00-概览.md) | 关键决策概览与 ADR 模板 |
| ADR-001：进水 60 tick、L 衰减 200 tick | [decisions/ADR-001-进水与衰减周期](decisions/ADR-001-进水与衰减周期.md) | #tick #进水 #衰减 |
| ADR-002：吸收延迟基准 15000 tick | [decisions/ADR-002-吸收延迟基准](decisions/ADR-002-吸收延迟基准.md) | #吸收延迟 |
| ADR-003：选侧「相同时先左」 | [decisions/ADR-003-选侧先左](decisions/ADR-003-选侧先左.md) | #选侧 #DrainForConsume |
| ADR-004：信息卡统计补丁移除 | [decisions/ADR-004-信息卡统计补丁移除](decisions/ADR-004-信息卡统计补丁移除.md) | #信息卡 #SpecialDisplayStats #RaceProperties |

---

## 按标签快速跳转

- `#双池`：见 [design/双池与PairIndex](design/双池与PairIndex.md)、[domain/00-概览](domain/00-概览.md)、[docs/泌乳系统逻辑图](docs/泌乳系统逻辑图.md)
- `#PairIndex`：见 [design/双池与PairIndex](design/双池与PairIndex.md)
- `#选侧`：见 [design/双池与PairIndex](design/双池与PairIndex.md)、[decisions/ADR-003-选侧先左](decisions/ADR-003-选侧先左.md)
- `#耐受`：见 [domain/代码常量与公式对应](domain/代码常量与公式对应.md)、[docs/泌乳系统逻辑图](docs/泌乳系统逻辑图.md) 第一、二、十二节
- `#EMDefOf`：见 [conventions/EMDefOf与代码位置](conventions/EMDefOf与代码位置.md)、[conventions/00-概览](conventions/00-概览.md)、.cursor/skills/rjw-cummilk-source
- `#tick`：见 [decisions/ADR-001-进水与衰减周期](decisions/ADR-001-进水与衰减周期.md)、[domain/代码常量与公式对应](domain/代码常量与公式对应.md)、[design/泌乳刷新策略与性能](design/泌乳刷新策略与性能.md)
- `#吸收延迟`：见 [decisions/ADR-002-吸收延迟基准](decisions/ADR-002-吸收延迟基准.md)、[domain/代码常量与公式对应](domain/代码常量与公式对应.md)
- `#信息卡`：见 [decisions/ADR-004-信息卡统计补丁移除](decisions/ADR-004-信息卡统计补丁移除.md)
- `#报错`：见 [docs/报错修复记录](docs/报错修复记录.md)（修错前先查）
- `#参数`、`#兼容`、`#坑点`：见 [domain/快速参考](domain/快速参考.md)
- `#流体系统`、`#一系统一目录`：见 [domain/mod定位-流体系统三大系统](domain/mod定位-流体系统三大系统.md)、[design/Source目录结构梳理](design/Source目录结构梳理.md)
- `#架构`、`#重组`：见 [design/架构原则与重组建议](design/架构原则与重组建议.md)

---

## 推荐标签表

新增条目时优先从下表选用标签，便于按标签聚合检索。

| 标签 | 含义 | 典型条目 |
|------|------|----------|
| #双池 | 左右池、多对、容量/流速/进水 | design/双池与PairIndex、domain/00-概览 |
| #PairIndex | 按对分组、同一对左右 | design/双池与PairIndex |
| #选侧 | 挤奶/吸奶先取哪一侧、preferLeft | design/双池与PairIndex、ADR-003 |
| #耐受 | E_tol、药物效果、GetProlactinToleranceFactor | domain/代码常量与公式对应、Docs 泌乳逻辑图 |
| #EMDefOf | 本 mod Def 引用方式 | conventions/EMDefOf与代码位置 |
| #tick | 30/200/2000 tick 周期 | ADR-001、domain/代码常量与公式对应 |
| #吸收延迟 | 服药后延迟生效、BaseAbsorptionDelayTicks | ADR-002、domain/代码常量与公式对应 |
| #信息卡 | Dialog_InfoCard、SpecialDisplayStats、不再注入统计 | decisions/ADR-004-信息卡统计补丁移除 |
| #参数 | 设置参数与行为联动 | domain/快速参考、[docs/参数联动表](docs/参数联动表.md) |
| #兼容 | 原版/RJW/其它 mod、只读只挂接 | domain/快速参考、[docs/与游戏本体及RJW的冲突与优化总结](docs/与游戏本体及RJW的冲突与优化总结.md) |
| #坑点 | 已知注意、存档、Def 顺序等 | domain/快速参考 |
| #审阅 | 代码审阅、逻辑验证、实现确认 | design/代码审阅-泌乳系统核心逻辑、design/代码审阅-药物系统实现确认 |
| #泌乳 | 泌乳系统、双池、进水算法 | design/代码审阅-泌乳系统核心逻辑、docs/泌乳系统逻辑图 |
| #药物 | 药物系统、吸收延迟、耐受公式 | design/代码审阅-药物系统实现确认、decisions/ADR-002-吸收延迟基准 |

（随条目增加可在此补充更多标签与链接）

---

**最后整理**：2026-03-14（新增代码审阅文档）
