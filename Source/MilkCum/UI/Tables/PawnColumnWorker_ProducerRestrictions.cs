using RimWorld;
using UnityEngine;
using Verse;
using MilkCum.UI;

namespace MilkCum.UI;

public class PawnColumnWorker_ProducerRestrictions : PawnColumnWorker
{
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (!ShouldShowFor(pawn)) return;
        if (Widgets.ButtonText(rect, "..."))
            Find.WindowStack.Add(new Window_ProducerRestrictions(pawn));
    }

    /// <summary>仅泌乳者（女性）显示「谁可以使用我的奶」；男性显示「谁可以吃我的精液制品」；非哺乳者不显示产奶限制列。</summary>
    internal static bool ShouldShowFor(Pawn pawn)
    {
        if (pawn?.CompEquallyMilkable() == null) return false;
        if (pawn.IsLactating()) return true;
        if (pawn.RaceProps?.Humanlike == true && pawn.gender == Gender.Male) return true;
        return false;
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.ProducerRestrictionsColumnTip".Translate();
    }

    public override int GetMinWidth(PawnTable table) => 36;
    public override int GetOptimalWidth(PawnTable table) => 140;
}
