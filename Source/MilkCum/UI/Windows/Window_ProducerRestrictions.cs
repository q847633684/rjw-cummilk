using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using MilkCum.Core.Settings;
using MilkCum.Core.Utils;
using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum.Leaking;
using MilkCum.Fluids.Lactation.Helpers;

namespace MilkCum.UI;

/// <summary>产奶者指定：谁可以使用我的奶（名单默认子女+伴侣）、谁可以使用我产出的奶/奶制品（默认仅自己）。筛选用：全部/殖民者/囚犯；表格列：姓名、关系、可以吸奶/挤奶、可以使用制品。</summary>
public class Window_ProducerRestrictions : Window
{
    private readonly Pawn producer;
    private readonly CompEquallyMilkable comp;
    private const float EntryHeight = 28f;
    private const float HeaderHeight = 24f;
    private const float CheckboxColWidth = 28f;
    private const float IconSize = 24f;
    private const float ScrollGap = 16f;
    private const float TabRowHeight = 36f;
    /// <summary>窄于此时「允许自己」改为两行布局（第一行挤奶+吃奶，第二行泄精+塞住）。</summary>
    private const float AllowSelfNarrowThreshold = 520f;

    /// <summary>筛选：控制表格中显示谁（全部 / 仅殖民者 / 仅囚犯）。</summary>
    private ProducerRestrictionFilter filter = ProducerRestrictionFilter.All;

    private Vector2 tableScrollPosition;

    private static readonly List<Pawn> tmpFiltered = new();
    private static readonly List<TabRecord> filterTabs = new();

    public Window_ProducerRestrictions(Pawn pawn)
    {
        producer = pawn;
        comp = pawn?.CompEquallyMilkable();
        closeOnClickedOutside = true;
        draggable = true;
        optionalTitle = "EM.ProducerRestrictionsTitle".Translate(producer?.LabelShort ?? "");
        absorbInputAroundWindow = true;
    }

    private enum ProducerRestrictionFilter
    {
        All,
        Colonists,
        Prisoners
    }

    /// <summary>按当前筛选填充列表（排除产奶者自己），不排序。调用方负责 Clear。使用 RimWorld 实际返回的 List&lt;Pawn&gt; 直接 for 遍历，避免 IEnumerable 枚举器分配。</summary>
    private void FillFilteredPawns(List<Pawn> outList)
    {
        if (producer?.Map == null || outList == null) return;
        var map = producer.Map;
        List<Pawn> source;
        switch (filter)
        {
            case ProducerRestrictionFilter.Colonists:
                source = map.mapPawns.FreeColonists;
                break;
            case ProducerRestrictionFilter.Prisoners:
                source = map.mapPawns.PrisonersOfColony;
                break;
            default:
                source = map.mapPawns.FreeColonistsAndPrisoners;
                break;
        }
        for (int i = 0; i < source.Count; i++)
        {
            Pawn p = source[i];
            if (p != producer && p != null && !p.Destroyed)
                outList.Add(p);
        }
    }

    private List<Pawn> GetFilteredPawnsSorted()
    {
        tmpFiltered.Clear();
        FillFilteredPawns(tmpFiltered);
        tmpFiltered.SortBy(x => x.LabelShort);
        return tmpFiltered;
    }

    /// <summary>产奶者与另一人的关系标签；无关系或异常时返回「—」。兼容无 GetGenderSpecificLabel 的 RimWorld 版本。</summary>
    private static string GetRelationLabel(Pawn producer, Pawn other)
    {
        if (producer == null || other == null) return "—";
        var rel = PawnRelationUtility.GetMostImportantRelation(producer, other);
        if (rel == null) return "—";
        try
        {
            return rel.GetGenderSpecificLabel(producer);
        }
        catch
        {
            return rel.label ?? "—";
        }
    }

    /// <summary>是否在「可以吸奶/挤奶」上显示为允许。名单非空时看名单；空时看 defaultSucklersWhenEmpty（若为 null 则现场取默认）。</summary>
    private bool IsAllowedSuckler(Pawn p, List<Pawn> defaultSucklersWhenEmpty)
    {
        if (comp?.allowedSucklers == null) return false;
        if (comp.allowedSucklers.Count > 0) return comp.allowedSucklers.Contains(p);
        if (defaultSucklersWhenEmpty != null) return defaultSucklersWhenEmpty.Contains(p);
        return MilkPermissionExtensions.GetDefaultSucklers(producer).Contains(p);
    }

    private void SetAllowedSuckler(Pawn p, bool allow)
    {
        if (comp == null) return;
        comp.EnsureSaveCompatAllowedLists();
        if (allow)
        {
            if (!comp.allowedSucklers.Contains(p))
            {
                comp.allowedSucklers.Add(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }
        else
        {
            if (comp.allowedSucklers.Count == 0)
            {
                foreach (Pawn x in MilkPermissionExtensions.GetDefaultSucklers(producer))
                    if (x != null && !x.Destroyed && (producer.MapHeld == null || x.MapHeld == null || x.MapHeld == producer.MapHeld) && !comp.allowedSucklers.Contains(x))
                        comp.allowedSucklers.Add(x);
            }
            if (comp.allowedSucklers.Remove(p))
                SoundDefOf.Click.PlayOneShotOnCamera();
        }
    }

    private bool IsAllowedConsumer(Pawn p) => comp?.allowedConsumers != null && comp.allowedConsumers.Contains(p);

    private void SetAllowedConsumer(Pawn p, bool allow)
    {
        if (comp == null) return;
        comp.EnsureSaveCompatAllowedLists();
        if (allow)
        {
            if (!comp.allowedConsumers.Contains(p))
            {
                comp.allowedConsumers.Add(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }
        else
        {
            if (comp.allowedConsumers.Remove(p))
                SoundDefOf.Click.PlayOneShotOnCamera();
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

        // 允许自己：挤奶(自己) | 泄精 | 塞住（仅当产主支持时显示）
        DrawAllowSelfSection(inRect.x, ref y, inRect.width);

        // 允许他人（他人对自己的操作）：筛选 Tab + 表格
        GUI.color = Color.gray;
        Widgets.Label(new Rect(inRect.x, y, inRect.width, HeaderHeight), "EM.RestrictionsAllowOthersSection".Translate());
        GUI.color = Color.white;
        y += HeaderHeight + 4f;

        // 筛选 Tab：全部 | 殖民者 | 囚犯
        Rect filterRow = new Rect(inRect.x, y, inRect.width, TabRowHeight);
        DrawFilterTabs(filterRow);
        y += TabRowHeight + 4f;

        bool showSucklerColumn = producer.gender == Gender.Female;
        bool forCumProducts = producer.gender == Gender.Male && !producer.IsLactating();
        bool showConsumersColumn = forCumProducts
            ? MilkCumSettings.IsProducerRestrictionConsumersEffectiveForCumProducts()
            : MilkCumSettings.IsProducerRestrictionConsumersEffectiveForMilkProducts();

        comp.EnsureSaveCompatAllowedLists();

        // 女性且名单为空时缓存默认吸奶名单，避免每行重复计算
        List<Pawn> cachedDefaultSucklers = null;
        if (showSucklerColumn && comp.allowedSucklers.Count == 0)
            cachedDefaultSucklers = MilkPermissionExtensions.GetDefaultSucklers(producer).ToList();

        // 表头：姓名 | 关系 | 可以吸奶/挤奶 | 可以使用制品
        float nameW = Mathf.Max(80f, inRect.width * 0.35f);
        float relW = Mathf.Min(80f, inRect.width * 0.2f);
        float suckleW = showSucklerColumn ? CheckboxColWidth + 4f : 0f;
        float consumeW = showConsumersColumn ? CheckboxColWidth + 4f : 0f;
        float rest = inRect.width - nameW - relW - suckleW - consumeW;
        if (rest < 0f) { relW += rest; rest = 0f; }
        else { nameW += rest; }

        GUI.color = Color.gray;
        Rect hName = new Rect(inRect.x, y, nameW, HeaderHeight);
        Widgets.Label(hName, "EM.RestrictionsTableHeaderName".Translate());
        Rect hRel = new Rect(hName.xMax, y, relW, HeaderHeight);
        Widgets.Label(hRel, "EM.RestrictionsTableHeaderRelation".Translate());
        float cx = hRel.xMax;
        if (showSucklerColumn)
        {
            Rect hSuckle = new Rect(cx, y, suckleW, HeaderHeight);
            Widgets.Label(hSuckle, "EM.RestrictionsTableHeaderSuckle".Translate());
            TooltipHandler.TipRegion(hSuckle, "EM.RestrictionsTableHeaderSuckleTip".Translate());
            cx = hSuckle.xMax;
        }
        if (showConsumersColumn)
        {
            Rect hConsume = new Rect(cx, y, consumeW, HeaderHeight);
            Widgets.Label(hConsume, "EM.RestrictionsTableHeaderConsume".Translate());
            TooltipHandler.TipRegion(hConsume, "EM.RestrictionsTableHeaderConsumeTip".Translate());
        }
        GUI.color = Color.white;
        y += HeaderHeight + 2f;

        Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
        y += 4f;

        List<Pawn> pawns = GetFilteredPawnsSorted();
        if (pawns.Count == 0)
        {
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, EntryHeight), "EM.RestrictionsFilterEmpty".Translate());
            Widgets.Label(new Rect(inRect.x, y + EntryHeight, inRect.width, EntryHeight), "EM.RestrictionsFilterEmptyHint".Translate());
            GUI.color = Color.white;
            return;
        }

        float bodyWidth = inRect.width - ScrollGap;
        float nW = Mathf.Max(80f, bodyWidth * 0.35f);
        float rW = Mathf.Min(80f, bodyWidth * 0.2f);
        float sW = showSucklerColumn ? CheckboxColWidth + 4f : 0f;
        float cW = showConsumersColumn ? CheckboxColWidth + 4f : 0f;
        float remainder = bodyWidth - nW - rW - sW - cW;
        if (remainder < 0f) rW += remainder;
        else nW += remainder;

        Rect scrollOutRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        Rect scrollViewRect = new Rect(0f, 0f, bodyWidth, pawns.Count * EntryHeight);
        Widgets.BeginScrollView(scrollOutRect, ref tableScrollPosition, scrollViewRect);

        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn p = pawns[i];
            float rowY = i * EntryHeight;
            Rect row = new Rect(0f, rowY, bodyWidth, EntryHeight);
            if (Mouse.IsOver(row))
                Widgets.DrawHighlight(row);
            Rect rName = new Rect(0f, rowY, nW, EntryHeight);
            Rect rRel = new Rect(nW, rowY, rW, EntryHeight);
            Widgets.ThingIcon(new Rect(rName.x, rName.y + (rName.height - IconSize) / 2f, IconSize, IconSize), p);
            Widgets.Label(new Rect(rName.x + IconSize + 4f, rName.y, rName.width - IconSize - 4f, rName.height), p.LabelCap);
            Widgets.Label(rRel, GetRelationLabel(producer, p));

            float checkX = rRel.xMax;
            if (showSucklerColumn)
            {
                Rect rSuckle = new Rect(checkX, rowY + (EntryHeight - CheckboxColWidth) / 2f, CheckboxColWidth, CheckboxColWidth);
                bool suckleChecked = IsAllowedSuckler(p, cachedDefaultSucklers);
                bool oldSuckle = suckleChecked;
                Widgets.Checkbox(rSuckle.position, ref suckleChecked, CheckboxColWidth);
                if (suckleChecked != oldSuckle)
                    SetAllowedSuckler(p, suckleChecked);
                checkX = rSuckle.xMax;
            }
            if (showConsumersColumn)
            {
                Rect rConsume = new Rect(checkX, rowY + (EntryHeight - CheckboxColWidth) / 2f, CheckboxColWidth, CheckboxColWidth);
                bool consumeChecked = IsAllowedConsumer(p);
                bool oldConsume = consumeChecked;
                Widgets.Checkbox(rConsume.position, ref consumeChecked, CheckboxColWidth);
                if (consumeChecked != oldConsume)
                    SetAllowedConsumer(p, consumeChecked);
            }
        }

        Widgets.EndScrollView();
    }

    /// <summary>绘制「允许自己」（对自己的操作）：挤奶、吃奶、泄精、塞住。窄屏时两行布局；每项带 Tooltip。</summary>
    private void DrawAllowSelfSection(float x, ref float y, float width)
    {
        Widgets.Label(new Rect(x, y, width, HeaderHeight), "EM.RestrictionsAllowSelfSection".Translate());
        y += HeaderHeight + 2f;

        float rowHeight = 24f;
        float gap = 12f;
        var sealComp = producer?.TryGetComp<Comp_SealCum>();
        bool adult = producer?.DevelopmentalStage == DevelopmentalStage.Adult;
        bool narrow = width < AllowSelfNarrowThreshold;

        // 第一行：挤奶、吃奶（窄屏时仅此一行；宽屏时泄精、塞住同行）
        float rowY = y;
        float cx = x;

        if (comp?.MilkSettings != null)
        {
            Rect r = new Rect(cx, rowY, 160f, rowHeight);
            bool val = producer.AllowMilkingSelf();
            bool oldVal = val;
            Widgets.CheckboxLabeled(r, "EM.RestrictionsAllowSelf_Milking".Translate(), ref val);
            TooltipHandler.TipRegion(r, "EM.RestrictionsAllowSelf_MilkingTip".Translate());
            if (val != oldVal) producer.SetAllowMilkingSelf(val);
            cx = r.xMax + gap;
        }

        if (comp?.MilkSettings != null)
        {
            Rect r = new Rect(cx, rowY, 140f, rowHeight);
            bool val = producer.AllowSelfConsumeProducts();
            bool oldVal = val;
            Widgets.CheckboxLabeled(r, "EM.RestrictionsAllowSelf_Consume".Translate(), ref val);
            TooltipHandler.TipRegion(r, "EM.RestrictionsAllowSelf_ConsumeTip".Translate());
            if (val != oldVal) producer.SetAllowSelfConsumeProducts(val);
            cx = r.xMax + gap;
        }

        if (narrow)
        {
            rowY += rowHeight + 4f;
            cx = x;
        }

        if (adult && sealComp != null && sealComp.PlayerControlled && CumflationUtility.CanBeCumflated(producer))
        {
            Rect r = new Rect(cx, rowY, 120f, rowHeight);
            bool val = sealComp.CanDeflate();
            bool oldVal = val;
            Widgets.CheckboxLabeled(r, "EM.Milk_AllowDeflate".Translate(), ref val);
            TooltipHandler.TipRegion(r, "EM.Milk_AllowDeflateTip".Translate());
            if (val != oldVal) sealComp.SetCanDeflate(val);
            cx = r.xMax + gap;
        }

        if (adult && sealComp != null && sealComp.canSeal())
        {
            Rect r = new Rect(cx, rowY, 100f, rowHeight);
            bool val = sealComp.IsSealed();
            bool oldVal = val;
            Widgets.CheckboxLabeled(r, "EM.Milk_SealCum".Translate(), ref val);
            TooltipHandler.TipRegion(r, "EM.Milk_SealCumTip".Translate());
            if (val != oldVal) sealComp.SetSealed(val);
        }

        y = rowY + rowHeight + 8f;
        Widgets.DrawLineHorizontal(x, y, width);
        y += 6f;
    }

    /// <summary>筛选 Tab：全部 | 殖民者 | 囚犯。</summary>
    private void DrawFilterTabs(Rect rect)
    {
        filterTabs.Clear();
        filterTabs.Add(new TabRecord("EM.RestrictionsFilterAll".Translate(), () => { filter = ProducerRestrictionFilter.All; SoundDefOf.Click.PlayOneShotOnCamera(); }, filter == ProducerRestrictionFilter.All));
        filterTabs.Add(new TabRecord("EM.RestrictionsFilterColonists".Translate(), () => { filter = ProducerRestrictionFilter.Colonists; SoundDefOf.Click.PlayOneShotOnCamera(); }, filter == ProducerRestrictionFilter.Colonists));
        filterTabs.Add(new TabRecord("EM.RestrictionsFilterPrisoners".Translate(), () => { filter = ProducerRestrictionFilter.Prisoners; SoundDefOf.Click.PlayOneShotOnCamera(); }, filter == ProducerRestrictionFilter.Prisoners));
        TabDrawer.DrawTabs(rect, filterTabs);
    }
}
