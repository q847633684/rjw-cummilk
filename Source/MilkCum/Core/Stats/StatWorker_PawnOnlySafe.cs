using RimWorld;
using Verse;

namespace MilkCum.Core.Stats;

/// <summary>
/// 仅在有 Pawn 的 StatRequest 下显示，避免信息卡在 StatsReportUtility.StatsToDraw 中枚举 StatDef 时
/// 对物品/Def 请求调用默认 StatWorker.ShouldShowFor(req) 导致 NRE / 列表错位（见 ADR-004、报错修复记录）。
/// 用于 DrugDurationMultiplier 等仅需在小人信息卡显示的统计。
/// </summary>
public class StatWorker_PawnOnlySafe : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        if (req.Pawn == null)
            return false;
        try
        {
            return base.ShouldShowFor(req);
        }
        catch
        {
            return false;
        }
    }
}
