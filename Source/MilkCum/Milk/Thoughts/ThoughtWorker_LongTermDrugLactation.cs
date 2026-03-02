using RimWorld;
using Verse;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Comps;

namespace MilkCum.Milk.Thoughts;

/// <summary>长期药物泌乳：药物诱发泌乳且持续超过约 15 天时显示情境心情。</summary>
public class ThoughtWorker_LongTermDrugLactation : ThoughtWorker
{
    private const int MinAgeTicksForLongTerm = 900000; // 15 游戏日

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p?.RaceProps?.Humanlike != true) return ThoughtState.Inactive;
        if (!p.IsLactating() || !p.HasDrugInducedLactation()) return ThoughtState.Inactive;
        var hediff = p.LactatingHediffWithComps();
        if (hediff == null || hediff.ageTicks < MinAgeTicksForLongTerm) return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(0);
    }
}
