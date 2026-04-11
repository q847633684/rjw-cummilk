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

    /// <summary>只要该 Pawn 有泌乳/等量挤奶组件就显示入口按钮：女性忽略当前是否泌乳，以便提前配置权限。</summary>
    internal static bool ShouldShowFor(Pawn pawn)
    {
        // 只要拥有等量挤奶组件，就允许打开“指定权限”窗口；不再区分性别/是否当前处于泌乳状态。
        return pawn?.CompEquallyMilkable() != null;
    }

    protected override string GetHeaderTip(PawnTable table)
    {
        return "EM.ProducerRestrictionsColumnTip".Translate();
    }

    public override int GetMinWidth(PawnTable table) => 36;
    public override int GetOptimalWidth(PawnTable table) => 140;
}
