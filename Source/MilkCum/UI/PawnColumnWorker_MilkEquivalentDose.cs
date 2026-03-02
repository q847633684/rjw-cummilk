using RimWorld;
using Verse;
using MilkCum.Milk.Helpers;

namespace MilkCum.UI;

/// <summary>殖民者列：当前等效剂量（只读，由 L 与公式推导）。</summary>
public class PawnColumnWorker_MilkEquivalentDose : PawnColumnWorker_Text
{
    protected override string GetTextFor(Pawn pawn)
    {
        if (!pawn.IsLactating()) return "-";
        var lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        var comp = lactating?.TryGetComp<HediffComp_EqualMilkingLactating>();
        if (comp == null) return "-";
        float L = comp.CurrentLactationAmount;
        float oneDoseL = 0.5f * PoolModelConstants.DoseToLFactor;
        if (oneDoseL <= 0f || L <= 0f) return "-";
        return (L / oneDoseL).ToString("F1");
    }
}
