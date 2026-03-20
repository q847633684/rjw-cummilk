using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace MilkCum.UI;
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
        ThingDef milkDef = pawn.MilkDef();
        if (milkDef == null) return;
        // 不打开 Dialog_InfoCard(milkDef)：该窗口在部分环境下会因内部列表 index 越界抛错；本 mod 已不再向信息卡注入统计（见 记忆库/decisions/ADR-004-信息卡统计补丁移除），故用 MessageBox 展示描述即可
        string desc = milkDef.description ?? milkDef.LabelCap;
        Find.WindowStack.Add(new Dialog_MessageBox(desc, "OK", null, null, null, milkDef.LabelCap));
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