using RimWorld;
using UnityEngine;
using Verse;
using EqualMilking.Helpers;
using EqualMilking.UI;

namespace EqualMilking;

public class PawnColumnWorker_ProducerRestrictions : PawnColumnWorker
{
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (!pawn.IsLactating() || pawn.CompEquallyMilkable() == null) return;
        if (Widgets.ButtonText(rect, "..."))
            Find.WindowStack.Add(new Window_ProducerRestrictions(pawn));
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.ProducerRestrictionsColumnTip".Translate();
    }

    public override int GetMinWidth(PawnTable table) => 36;
    public override int GetOptimalWidth(PawnTable table) => 44;
}
