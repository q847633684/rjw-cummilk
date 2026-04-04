# 记忆库变更记录

本文件记录**记忆库本身**的新增、废弃与重要修订，便于追溯。格式：日期 + 变更类型 + 条目/说明。

---

## 2026-04-03

- **新增**：`design/拟真子系统开关清单与优先级.md` — 拟真向子系统 SYS-01～09 总表、P0/P1/P2 落地顺序、代谢门控优先级草案、与用户口述需求的对照表；`index.md` design 节已登记。

---

## 2026-03-14

- **新增**：代码审阅文档。基于实际代码审查，新增两个设计决策文档：
  - `design/代码审阅-泌乳系统核心逻辑.md`：审查 `CompEquallyMilkable` 和 `UpdateMilkPools` 实现
  - `design/代码审阅-药物系统实现确认.md`：确认吸收延迟、耐受公式等药物系统实现
  
- **更新**：索引文件 `index.md`：
  - 在 design 部分添加两个新审阅文档的链接
  - 在标签表中新增 `#审阅`、`#泌乳`、`#药物` 标签
  - 更新推荐标签表，便于按主题检索
  
- **确认**：通过代码审查验证记忆库设计与实际实现的一致性：
  - 确认吸收延迟 15000 tick 基准正确实现
  - 确认耐受公式 `E_tol(t) = [max(1−t, 0.05)]^exponent` 准确实现
  - 确认双池系统、定时器系统等核心逻辑与设计一致

## 2026-03-24

- **清理**：修复记忆库中的无效/过时链接与占位链接，消除整理时的“假链接噪音”。
  - `design/泌乳刷新策略与性能.md`：`decisions/ADR-001...` 改为正确相对路径 `../decisions/...`
  - `design/激素模型-催乳素与排乳反馈.md`：`docs/...` 改为 `../docs/...`
  - `domain/mod定位-流体系统三大系统.md`：`design/...` 改为 `../design/...`
  - `domain/快速参考.md`：旧 `../../Docs/...` 迁移路径改为 `../docs/...`
- **整理**：将模板中的占位超链接改为纯文本/代码样式，避免出现不可点击的伪链接：
  - `README.md`、`建议与改进.md`：占位链接写法改为「新条目（相对路径）」
  - `schema.md`：模板示例改为代码样式 `记忆库/docs/xxx.md#锚点`，不再使用占位超链接
- **补全索引**：将 `docs/RJW乳房定义与rjw-cummilk使用说明.md` 收录到 `index.md` 与 `docs/README.md`。
- **一致性修正**：`docs/RJW乳房定义与rjw-cummilk使用说明.md` 修正文内前后矛盾（补充 `TryGetBreastSize(...).weight` 的实际使用场景），并增加「最后与代码核对」。
- **激进清理（UI 历史方案收口）**：删除重复且与当前实现冲突的旧 UI 方案文档，保留单一入口与审阅结论。
  - 删除：`design/模组设置UI系统整理与优化方案.md`
  - 删除：`design/模组设置窗口完整UI结构.md`
  - 删除：`design/模组设置UI专业级重构方案.md`
  - 删除：`docs/设置UI模拟显示.md`
  - 同步：`index.md`、`docs/README.md`、`docs/设置UI重构框架.md` 移除/替换上述引用，统一改为以 `design/UI审阅结论.md` + `docs/设置UI重构框架.md` 为入口。

## 2026-03-25

- **乳池基容量模式**：代码与 UI 仅保留三档（严重度 / 重量 / 体积），已移除取大与混合；无旧档精细迁移承诺。Keyed 中已删未再使用的模式标签；`RJW乳房定义与rjw-cummilk使用说明.md` 中与「取大/混合」相关的表述已对齐。

## 2026-03-24

- **修正**：`docs/RJW乳房定义与rjw-cummilk使用说明.md` 中关于 `PartSizeCalculator` 的描述与现代码不一致，已改为：
  - `TryGetBreastSize` 在容量模式为「重量」时用于读取 `weight`
  - `TryGetCupSize` 等其余接口未使用
- **整理**：将该文档补充进 `docs/README.md` 与 `index.md` 的 docs 索引，避免“存在但不可检索”的无效记忆。

## 2026-03

- **整理**：记忆库索引与 docs 入口统一整理。① **docs/README.md**：补全 docs 下全部文档索引，按「泌乳系统 / 参数与清单 / 冲突与报错 / UI与文案 / 建议与待办 / 模拟与脚本」分类，并增加快速查找项（报错记录、Patch/集成）。② **index.md**：新增「记忆库根目录」节，收录 README、架构说明、建议与改进、changelog-memory 的链接与说明；docs 节「其它」说明补充「报错记录、Integrations」；按标签快速跳转增加 `#报错` → docs/报错修复记录。

- **整理（补做 2/4/5/6）**：① 命名与收口约定：README「维护」下新增「命名与收口约定」表。② 去重与收口：docs/README 中 Source 相关两条加互补说明；design/00-概览 引用改为 记忆库/docs/。③ 约定与保鲜：多处加「最后整理：2026-03」；建议与改进新增「六、整理时做哪些事」checklist。④ 清理与优化：链接格式已统一。

---

## 2025-03（续）

- **迁移**：将项目根目录 `Docs/` 全部迁入 `记忆库/docs/`，规格文档与记忆库统一存放；更新 README、架构说明、schema、index、domain/design/decisions 及规则中对 Docs 的引用为 `记忆库/docs/`；**删除**原 `Docs/` 下所有已迁移文件，仅保留 `Docs/README.md` 作重定向。约定写入规则与 conventions：迁移后须删除旧文件，不保留双份。

## 2025-03

- **新增**：记忆库初始结构（README、架构说明、schema、index、design/conventions/domain/decisions 及 00-概览）。
- **新增**：design/双池与PairIndex；conventions/EMDefOf与代码位置；domain/代码常量与公式对应、domain/快速参考；decisions/ADR-001～ADR-003。
- **新增**：.cursor/rules/use-memory-bank.mdc（AI 使用记忆库指令）。
- **约定**：重要条目增加「最后与代码核对」；schema 增加废弃与替代约定；index 增加推荐标签表。
- **约定**：README/架构说明注明「当前记忆默认针对 1.6」；README 增加检索入口句（先看 index + domain/00-概览）。
- **新增**：本 changelog-memory.md。
