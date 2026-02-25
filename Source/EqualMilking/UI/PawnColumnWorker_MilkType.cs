using RimWorld;
using UnityEngine;
using Verse;
using EqualMilking.Helpers;
using System.Collections.Generic;

namespace EqualMilking;
public class PawnColumnWorker_MilkType : PawnColumnWorker_Icon
{
    private static readonly Dictionary<Pawn, Texture2D> IconCache = new();
    protected override Texture2D GetIconFor(Pawn pawn)
    {
        if (IconCache.TryGetValue(pawn, out Texture2D icon)) { return icon; }
        IconCache[pawn] = pawn.MilkDef()?.uiIcon;
        return IconCache[pawn];
    }
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.MilkType + "\n\n" + Lang.MilkTypeDesc + "\n\n" + base.GetHeaderTip(table);
    }
    protected override string GetIconTip(Pawn pawn)
    {
        return pawn.MilkDef()?.LabelCap;
    }
    protected override void ClickedIcon(Pawn pawn)
    {
        base.ClickedIcon(pawn);
        if (pawn.MilkDef() == null) return;
        Find.WindowStack.Add(new Dialog_InfoCard(pawn.MilkDef()));
    }
    /// <summary>
    /// Called once on table dirty, clear cache
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public override int GetMinHeaderHeight(PawnTable table)
    {
        IconCache.Clear();
        return base.GetMinHeaderHeight(table);
    }
}