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
        if (!ShouldShowFor(pawn)) return;
        if (Widgets.ButtonText(rect, "..."))
            Find.WindowStack.Add(new Window_ProducerRestrictions(pawn));
    }

    /// <summary>泌乳者或（Cumpilation 启用时的）人形均可指定谁可食用我产出的奶/精液。</summary>
    internal static bool ShouldShowFor(Pawn pawn)
    {
        if (pawn?.CompEquallyMilkable() == null) return false;
        if (pawn.IsLactating()) return true;
        if (ModLister.GetActiveModWithIdentifier("vegapnk.cumpilation") != null && pawn.RaceProps?.Humanlike == true)
            return true;
        return false;
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.ProducerRestrictionsColumnTip".Translate();
    }

    public override int GetMinWidth(PawnTable table) => 36;
    public override int GetOptimalWidth(PawnTable table) => 44;
}
