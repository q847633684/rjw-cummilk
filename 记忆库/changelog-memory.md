# 记忆库变更记录

本文件记录**记忆库本身**的新增、废弃与重要修订，便于追溯。格式：日期 + 变更类型 + 条目/说明。

---

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
