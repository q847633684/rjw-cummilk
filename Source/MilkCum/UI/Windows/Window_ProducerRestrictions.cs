using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using static MilkCum.Core.Utils.Lang;

namespace MilkCum.UI;

/// <summary>产奶者指定：谁可以使用我的奶（名单默认子女+伴侣）、谁可以使用我产出的奶/奶制品（默认仅自己）。</summary>
public class Window_ProducerRestrictions : Window
{
    private readonly Pawn producer;
    private readonly CompEquallyMilkable comp;
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
        optionalTitle = "EM.ProducerRestrictionsTitle".Translate(producer?.LabelShort ?? "");
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
        // 女性：始终显示两个选项框——（1）谁可以使用我的奶 （2）谁可以吃我的奶制品。男性只显示精液制品。
        if (producer.gender == Gender.Female)
        {
            comp.EnsureSaveCompatAllowedLists();
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight), "EM.WhoCanSuckleFromMe".Translate());
            y += EntryHeight;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight * 0.8f), "EM.WhoCanSuckleFromMeDefault".Translate());
            GUI.color = Color.white;
            y += EntryHeight;
            // 名单为空时用默认（子女+伴侣）作为显示，实际仍修改 comp.allowedSucklers，点「禁止」时再落盘
            List<Pawn> sucklerDisplay = comp.allowedSucklers.Count > 0 ? comp.allowedSucklers : MilkPermissionExtensions.GetDefaultSucklers(producer);
            DrawListSection(inRect, ref y, sucklerDisplay, comp.allowedSucklers, Allow, Forbid, ColonyPawns().ToList(), null);
            y += SeparatorHeight * 2;
            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
            y += SeparatorHeight * 2;
        }

        // 谁可以吃我的奶制品/精液制品：仅当奶标签里对应物品种类开启「显示动物名」时生效，否则完全隐藏该区块
        bool forCumProducts = producer.gender == Gender.Male && !producer.IsLactating();
        bool showConsumersSection = forCumProducts
            ? MilkCumSettings.IsProducerRestrictionConsumersEffectiveForCumProducts()
            : MilkCumSettings.IsProducerRestrictionConsumersEffectiveForMilkProducts();
        if (showConsumersSection)
        {
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight), forCumProducts ? "EM.WhoCanUseMyCumProducts".Translate() : "EM.WhoCanUseMyMilkProducts".Translate());
            y += EntryHeight;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight * 0.8f),
                forCumProducts ? "EM.WhoCanUseMyCumProductsDefault".Translate() : "EM.WhoCanUseMyMilkProductsDefault".Translate());
            GUI.color = Color.white;
            y += EntryHeight;
            DrawListSection(inRect, ref y, comp.allowedConsumers, comp.allowedConsumers, Allow, Forbid, ColonyPawns().ToList(), null);
        }
    }

    /// <param name="displayList">用于显示「已允许」的名单；可与 modifyList 相同，或为空时用默认名单以仅显示。</param>
    /// <param name="modifyList">实际被「允许/禁止」按钮修改的名单。</param>
    private void DrawListSection(Rect inRect, ref float y, List<Pawn> displayList, List<Pawn> modifyList, string allowLabel, string forbidLabel, List<Pawn> allCandidates, HashSet<Pawn> exclude)
    {
        if (displayList == null || modifyList == null) return;
        var assigned = new List<Pawn>(displayList);
        var unassigned = allCandidates.Where(p => !displayList.Contains(p) && (exclude == null || !exclude.Contains(p))).ToList();
        tmpSorted.Clear();
        tmpSorted.AddRange(assigned);
        tmpSorted.SortBy(x => x.LabelShort);
        float h = EntryHeight;
        foreach (Pawn p in tmpSorted)
        {
            Rect row = new(inRect.x, y, inRect.width, h);
            if (Widgets.ButtonText(new Rect(row.xMax - ButtonWidth - 4f, row.y, ButtonWidth, h - 2f), forbidLabel))
            {
                if (modifyList.Count == 0 && displayList != modifyList)
                {
                    foreach (Pawn x in displayList)
                        if (x != null && !x.Destroyed && (producer.MapHeld == null || x.MapHeld == null || x.MapHeld == producer.MapHeld) && !modifyList.Contains(x))
                            modifyList.Add(x);
                }
                modifyList.Remove(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            Widgets.ThingIcon(new Rect(row.x, row.y, h, h), p);
            float labelWidth = Mathf.Max(0f, row.width - h - ButtonWidth - 12f);
            Widgets.Label(new Rect(row.x + h + 4f, row.y, labelWidth, h), p.LabelCap);
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
                modifyList.Add(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            Widgets.ThingIcon(new Rect(row.x, row.y, h, h), p);
            float labelWidth = Mathf.Max(0f, row.width - h - ButtonWidth - 12f);
            Widgets.Label(new Rect(row.x + h + 4f, row.y, labelWidth, h), p.LabelCap);
            y += h;
        }
    }
}
