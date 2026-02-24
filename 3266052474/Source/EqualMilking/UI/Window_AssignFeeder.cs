using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using EqualMilking.Helpers;
namespace EqualMilking.UI;
public class Window_AssignFeeder : Window
{
    private readonly Pawn fedPawn;
    private readonly CompEquallyMilkable compEquallyMilkable;
    private Vector2 scrollPosition;

    private const float EntryHeight = 35f;

    private const int AssignButtonWidth = 165;

    private const int SeparatorHeight = 7;

    private static readonly List<Pawn> tmpPawnSorted = new();
    private void SortTmpList(IEnumerable<Pawn> collection)
    {
        tmpPawnSorted.Clear();
        tmpPawnSorted.AddRange(collection);
        tmpPawnSorted.SortBy(x => x.LabelShort);
    }
    public Window_AssignFeeder(Pawn pawn)
    {
        this.fedPawn = pawn;
        this.compEquallyMilkable = pawn.GetComp<CompEquallyMilkable>();
        this.closeOnClickedOutside = true;
        this.draggable = true;
    }
    public HashSet<Pawn> GetLactatingPawns()
    {
        return ListerAsync.AllColonyPawns(this.fedPawn.Map).Where(p => p.IsLactating() && p.CanBreastfeedEver(this.fedPawn) && p.AllowBreastFeedByAge(this.fedPawn)).ToHashSet();
    }
    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40f), Lang.AutofeedSetting);
        HashSet<Pawn> lactatingPawns = GetLactatingPawns();
        List<Pawn> assignedPawns = new(compEquallyMilkable.assignedFeeders);
        HashSet<Pawn> unassignedPawns = new(lactatingPawns);
        unassignedPawns.ExceptWith(assignedPawns);
        inRect.y += 20f;
        inRect.height -= 20f;
        Text.Font = GameFont.Small;
        Rect outRect = new(inRect);
        outRect.yMin += 20f;
        outRect.yMax -= 40f;
        float num = 0f;
        num += (float)lactatingPawns.Count * EntryHeight;
        num += SeparatorHeight;
        num += EntryHeight;
        num += assignedPawns.Count > 0 ? SeparatorHeight + EntryHeight : 0f;
        Rect viewRect = new(0f, 0f, outRect.width, num);
        Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        SortTmpList(assignedPawns);
        float y = 0f;
        Widgets.Label(new Rect(0f, y, viewRect.width, EntryHeight), Lang.Allow + ":");
        y += EntryHeight;
        for (int i = 0; i < tmpPawnSorted.Count; i++)
        {
            Pawn pawn = tmpPawnSorted[i];
            DrawAssignedRow(pawn, ref y, viewRect, i);
        }

        if (assignedPawns.Count > 0)
        {
            Rect r = new(0f, y, viewRect.width, SeparatorHeight);
            y += SeparatorHeight;
            using (new TextBlock(Widgets.SeparatorLineColor))
            {
                Widgets.DrawLineHorizontal(r.x, r.y + r.height / 2f, r.width);
            }
            Widgets.Label(new Rect(0f, y, viewRect.width, EntryHeight), Lang.Forbid + ":");
            y += EntryHeight;
        }

        SortTmpList(unassignedPawns);
        for (int j = 0; j < tmpPawnSorted.Count; j++)
        {
            Pawn pawn2 = tmpPawnSorted[j];
            DrawUnassignedRow(pawn2, ref y, viewRect, j);
        }
        tmpPawnSorted.Clear();
        Widgets.EndScrollView();
    }


    private void DrawAssignedRow(Pawn pawn, ref float y, Rect viewRect, int i)
    {
        Rect rect = new(0f, y, viewRect.width, EntryHeight);
        y += EntryHeight;
        if (i % 2 == 1)
        {
            Widgets.DrawLightHighlight(rect);
        }

        Rect rect2 = rect;
        rect2.width = rect.height;
        Widgets.ThingIcon(rect2, pawn);
        Rect rect3 = rect;
        rect3.xMin = rect.xMax - AssignButtonWidth - 10f;
        rect3 = rect3.ContractedBy(2f);
        if (Widgets.ButtonText(rect3, Lang.Unassign))
        {
            compEquallyMilkable.assignedFeeders.Remove(pawn);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        Rect rect4 = rect;
        rect4.xMin = rect2.xMax + 10f;
        rect4.xMax = rect3.xMin - 10f;
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.LabelEllipses(rect4, pawn.LabelCap);
        }
    }

    private void DrawUnassignedRow(Pawn pawn, ref float y, Rect viewRect, int i)
    {
        bool isLactating = pawn.IsLactating();
        Rect rect = new(0f, y, viewRect.width, EntryHeight);
        y += EntryHeight;
        if (i % 2 == 1)
        {
            Widgets.DrawLightHighlight(rect);
        }
        if (!isLactating)
        {
            GUI.color = Color.gray;
        }
        Rect rect2 = rect;
        rect2.width = rect.height;
        Widgets.ThingIcon(rect2, pawn);
        Rect rect3 = rect;
        rect3.xMin = rect.xMax - AssignButtonWidth - 10f;
        rect3 = rect3.ContractedBy(2f);
        if (isLactating)
        {
            if (Widgets.ButtonText(rect3, Lang.Assign))
            {
                compEquallyMilkable.assignedFeeders.Add(pawn);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        Rect rect5 = rect;
        rect5.xMin = rect2.xMax + 10f;
        rect5.xMax = rect3.xMin - 10f;
        string label = pawn.LabelCap + (isLactating ? "" : (" (" + Lang.Lactating + ")"));
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.LabelEllipses(rect5, label);
        }
    }

}
