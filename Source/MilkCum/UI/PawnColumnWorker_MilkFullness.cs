using RimWorld;
using Verse;
using MilkCum.Milk.Helpers;

namespace MilkCum.UI;
public class PawnColumnWorker_MilkFullness : PawnColumnWorker_Text
{
    protected override string GetTextFor(Pawn pawn)
    {
        if (!pawn.IsLactating()) { return "-"; }
        return pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating).TryGetComp<HediffComp_EqualMilkingLactating>().Charge.ToStringPercent();
    }
}