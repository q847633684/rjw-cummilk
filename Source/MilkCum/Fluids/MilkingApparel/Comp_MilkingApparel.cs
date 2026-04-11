using RimWorld;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

public class Comp_MilkingApparel : ThingComp
{
    private CompProperties_MilkingApparel PropsTyped => (CompProperties_MilkingApparel)props;

    public override void Notify_Equipped(Pawn pawn)
    {
        if (pawn == null || PropsTyped.hediffWhileWorn == null) return;
        if (pawn.health.hediffSet.HasHediff(PropsTyped.hediffWhileWorn)) return;
        pawn.health.AddHediff(PropsTyped.hediffWhileWorn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn == null || PropsTyped.hediffWhileWorn == null) return;
        Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(PropsTyped.hediffWhileWorn);
        if (h != null) pawn.health.RemoveHediff(h);
    }
}
