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
| 耐受系统重构设计 | [docs/耐受系统重构设计](docs/耐受系统重构设计.md) | E_tol、进水/衰减公式、落地修改清单 |
| 与游戏本体及RJW的冲突与优化总结 | [docs/与游戏本体及RJW的冲突与优化总结](docs/与游戏本体及RJW的冲突与优化总结.md) | 冲突与优化 |
| UI 文案前后对照 | [docs/UI文案前后对照](docs/UI文案前后对照.md) | 术语统一、健康页悬停、硬编码冒号、改文建议与实施顺序 |
| 其它 | 见 [docs/README](docs/README.md) | 待办、建议评估、模拟、设置UI、脚本等 |

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
- `#参数`、`#兼容`、`#坑点`：见 [domain/快速参考](domain/快速参考.md)

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

（随条目增加可在此补充更多标签与链接）
