using RimWorld;
using UnityEngine;
using Verse;
using EqualMilking.Helpers;

namespace EqualMilking;
public abstract class PawnColumnWorker_MilkProducer : PawnColumnWorker_Checkbox
{
    protected override bool HasCheckbox(Pawn pawn)
    {
        return pawn.IsMilkable() && pawn.CompEquallyMilkable() is CompEquallyMilkable comp && comp.MilkSettings != null;
    }
    protected virtual bool IsDisabled(Pawn pawn)
    {
        return !pawn.IsLactating();
    }
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (IsDisabled(pawn))
        {
            Vector2 topLeft = new(rect.x + ((rect.width - 24f) / 2f), rect.y + 3f);
            //SetValue(pawn, false, table);
            bool checkOn = GetValue(pawn);
            Widgets.Checkbox(topLeft, ref checkOn, 24f, disabled: true, def.paintable);

            Rect tipRect = new(topLeft.x, topLeft.y, 24f, 24f);
            if (Mouse.IsOver(tipRect))
            {
                string tip = GetTip(pawn);
                if (!tip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(tipRect, tip);
                }
            }
            if (Widgets.ButtonInvisible(new Rect(topLeft.x, topLeft.y, 24f, 24f)))
            {
                SetValue(pawn, !checkOn, table);
            }
            return;
        }
        base.DoCell(rect, pawn, table);
    }
    protected override string GetTip(Pawn pawn)
    {
        if (!pawn.IsLactating())
        {
            return "BreastfeedFailReason_MomNotEnoughMilk".Translate().Replace("{MOM_labelShort}", pawn.LabelShort);
        }
        return base.GetTip(pawn);
    }
}