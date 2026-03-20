using RimWorld;
using Verse;
using MilkCum.Core;

namespace MilkCum.Fluids.Lactation.Thoughts;

/// <summary>长期药物泌乳：药物诱发泌乳且持续超过�?15 天时显示情境心情</summary>
public class ThoughtWorker_LongTermDrugLactation : ThoughtWorker
{
    private const int MinAgeTicksForLongTerm = 900000; // 15 游戏�?

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p?.RaceProps?.Humanlike != true) return ThoughtState.Inactive;
        if (!p.IsLactating() || !p.HasDrugInducedLactation()) return ThoughtState.Inactive;
        var hediff = p.LactatingHediffWithComps();
        if (hediff == null || hediff.ageTicks < MinAgeTicksForLongTerm) return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(0);
    }
}
