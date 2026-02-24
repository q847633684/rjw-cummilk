using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using EqualMilking.Helpers;
using static EqualMilking.Helpers.Lang;

namespace EqualMilking.UI;

/// <summary>产奶者指定：谁可以吸我的奶（默认子女+伴侣）、谁可以使用我产出的奶/奶制品（默认仅自己）。</summary>
public class Window_ProducerRestrictions : Window
{
    private readonly Pawn producer;
    private readonly CompEquallyMilkable comp;
    private Vector2 scrollPosition;
    private const float EntryHeight = 32f;
    private const int ButtonWidth = 120;
    private const int SeparatorHeight = 6;
    private static readonly List<Pawn> tmpSorted = new();

    public Window_ProducerRestrictions(Pawn pawn)
    {
        producer = pawn;
        comp = pawn?.CompEquallyMilkable();
        closeOnClickedOutside = true;
        draggable = true;
    }

    private IEnumerable<Pawn> ColonyPawns()
    {
        if (producer?.Map == null) yield break;
        foreach (Pawn p in producer.Map.mapPawns.FreeColonistsAndPrisoners)
        {
            if (p != producer) yield return p;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (comp == null) return;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 36f), "EM.ProducerRestrictionsTitle".Translate(producer.LabelShort));
        inRect.y += 40f;
        Text.Font = GameFont.Small;

        float y = inRect.y;
        // 女性（泌乳者）：谁可以吸我的奶
        if (producer.IsLactating())
        {
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight), "EM.WhoCanSuckleFromMe".Translate());
            y += EntryHeight;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight * 0.8f), "EM.WhoCanSuckleFromMeDefault".Translate());
            GUI.color = Color.white;
            y += EntryHeight;
            DrawListSection(inRect, ref y, comp.allowedSucklers, Allow, Forbid, ColonyPawns().ToList(), null);
            y += SeparatorHeight * 2;
            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
            y += SeparatorHeight * 2;
        }

        // 女性（泌乳者）：谁可以吃我的奶制品
        // 男性（非泌乳，Cumpilation 时）：谁可以吃我的精液制品
        bool forCumProducts = !producer.IsLactating();
        Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight), forCumProducts ? "EM.WhoCanUseMyCumProducts".Translate() : "EM.WhoCanUseMyMilkProducts".Translate());
        y += EntryHeight;
        GUI.color = Color.gray;
        Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight * 0.8f), forCumProducts ? "EM.WhoCanUseMyCumProductsDefault".Translate() : "EM.WhoCanUseMyMilkProductsDefault".Translate());
        GUI.color = Color.white;
        y += EntryHeight;
        DrawListSection(inRect, ref y, comp.allowedConsumers, Allow, Forbid, ColonyPawns().ToList(), null);
    }

    private void DrawListSection(Rect inRect, ref float y, List<Pawn> list, string allowLabel, string forbidLabel, List<Pawn> allCandidates, HashSet<Pawn> exclude)
    {
        if (list == null) return;
        var assigned = new List<Pawn>(list);
        var unassigned = allCandidates.Where(p => !list.Contains(p) && (exclude == null || !exclude.Contains(p))).ToList();
        tmpSorted.Clear();
        tmpSorted.AddRange(assigned);
        tmpSorted.SortBy(x => x.LabelShort);
        float h = EntryHeight;
        foreach (Pawn p in tmpSorted)
        {
            Rect row = new(inRect.x, y, inRect.width, h);
            if (Widgets.ButtonText(new Rect(row.xMax - ButtonWidth - 4f, row.y, ButtonWidth, h - 2f), forbidLabel))
            {
                list.Remove(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            Widgets.ThingIcon(new Rect(row.x, row.y, h, h), p);
            Widgets.Label(new Rect(row.x + h + 4f, row.y, row.width - h - ButtonWidth - 12f, h), p.LabelCap);
            y += h;
        }
        y += SeparatorHeight;
        tmpSorted.Clear();
        tmpSorted.AddRange(unassigned);
        tmpSorted.SortBy(x => x.LabelShort);
        foreach (Pawn p in tmpSorted)
        {
            Rect row = new(inRect.x, y, inRect.width, h);
            if (Widgets.ButtonText(new Rect(row.xMax - ButtonWidth - 4f, row.y, ButtonWidth, h - 2f), allowLabel))
            {
                list.Add(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            Widgets.ThingIcon(new Rect(row.x, row.y, h, h), p);
            Widgets.Label(new Rect(row.x + h + 4f, row.y, row.width - h - ButtonWidth - 12f, h), p.LabelCap);
            y += h;
        }
    }
}
