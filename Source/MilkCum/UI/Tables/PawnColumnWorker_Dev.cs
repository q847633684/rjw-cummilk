using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.UI;
public class PawnColumnWorker_Dev : PawnColumnWorker
{
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (Widgets.ButtonText(rect.LeftHalf(), "+"))
        {
            if (!pawn.health.hediffSet.HasHediff(HediffDefOf.Lactating))
            {
                pawn.health.AddHediff(HediffDefOf.Lactating, pawn.GetBreastOrChestPart());
            }
            else
            {
                pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating).Severity += 1f;
            }
        }
        if (Widgets.ButtonText(rect.RightHalf(), "-"))
        {
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Lactating))
            {
                Hediff lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
                lactating.Severity -= 1f;
                if (lactating.Severity <= 0) { pawn.health.RemoveHediff(lactating); }
            }
        }
    }
    public override bool VisibleCurrently => Prefs.DevMode && DebugSettings.godMode;
}