using RimWorld;
using UnityEngine;
using Verse;
using EqualMilking.Helpers;
using EqualMilking.UI;

namespace EqualMilking;
[StaticConstructorOnStartup]
public class PawnColumnWorker_FedBy : PawnColumnWorker_Checkbox
{
    protected override string GetHeaderTip(PawnTable table)
    {
        return Lang.AutofeedSetting+ "\n\n" + base.GetHeaderTip(table);
    }

    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (!HasCheckbox(pawn)) { return; }
        Vector2 topLeft = new(rect.x + 2f, rect.y + 3f);
        Rect checkboxRect = new(topLeft.x, topLeft.y, 24f, 24f);
        bool checkOn = GetValue(pawn);
        bool flag = checkOn;
        Widgets.Checkbox(topLeft, ref checkOn, 24f, disabled: false, def.paintable);
        if (Mouse.IsOver(checkboxRect))
        {
            string tip = GetTip(pawn);
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(checkboxRect, tip);
            }
        }
        if (checkOn != flag)
        {
            SetValue(pawn, checkOn, table);
        }
        if (this.GetValue(pawn))
        {
            if (Widgets.ButtonText(rect.RightHalf(), "..."))
            {
                Find.WindowStack.Add(new Window_AssignFeeder(pawn));
            }
        }
    }

    protected override bool GetValue(Pawn pawn) => pawn.AllowToBeFed();
    protected override void SetValue(Pawn pawn, bool value, PawnTable table) => pawn.SetAllowToBeFed(value);
    public override int GetMinWidth(PawnTable table)
    {
        return 2 * base.GetMinWidth(table);
    }
}