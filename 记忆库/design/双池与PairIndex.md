# [design] 双池结构与 PairIndex、选侧规则

- **type**: design
- **date**: 2025-03（根据当前代码）
- **tags**: #双池 #PairIndex #选侧 #GetBreastPoolEntries #DrainForConsume
- **related_docs**: [Docs/泌乳系统逻辑图.md](../../Docs/泌乳系统逻辑图.md) 第一、三、五节
- **最后与代码核对**: 2025-03

## Context

需要支持「多对乳房」且每对独立左右池：容量、流速、撑大、挤奶/吸奶取奶顺序都按「对」组织；未启用 RJW 乳房尺寸时退化为单对（左右各 0.5）。

## Conclusion

- **结构**：`ExtensionHelper.GetBreastPoolEntries(pawn)` 按「每个 RJW 乳房 hediff = 一对」生成 `BreastPoolEntry` 列表；每对产生 key_L、key_R 两条，同一对共享 **PairIndex**（从 0 递增）。未启用 RJW 或空列表时返回单对 `Left_Default` / `Right_Default`，容量与流速各 0.5。
- **进水**：`CompEquallyMilkable` 内 `entries.GroupBy(e => e.PairIndex).OrderBy(g => g.Key)` 按对分组，每对用 `LactationPoolState.TickGrowth(flowLeft, flowRight, ...)` 进水；撑大仅当该对两侧都达基础容量后才允许向 1.2× 撑大。
- **取奶**：`DrainForConsume(amount)` 按 `byPair` 总满度从高到低排序，每对内比较左右水位，**先取较满一侧**；**两侧相同时先左**（`preferLeft = true`），与性别/种族无关。

## Rationale

- PairIndex 保证「一对」在进水与取奶时一致，避免左右错配；撑大按对判定与规格「仅当该对两侧都达基础容量」一致。
- 选侧「相同时先左」在代码中固定为 `preferLeft = true`（`CompEquallyMilkable` 约 544 行），便于行为可预测与测试。

## Related files

- `Source/MilkCum/Milk/Helpers/ExtensionHelper.cs`：`GetBreastPoolEntries`
- `Source/MilkCum/Milk/Data/BreastPoolEntry.cs`：Key, Capacity, FlowMultiplier, IsLeft, PairIndex
- `Source/MilkCum/Milk/Data/LactationPoolState.cs`：`TickGrowth`
- `Source/MilkCum/Milk/Comps/CompEquallyMilkable.cs`：`UpdateMilkPools`（byPair + TickGrowth）、`DrainForConsume`（preferLeft）
