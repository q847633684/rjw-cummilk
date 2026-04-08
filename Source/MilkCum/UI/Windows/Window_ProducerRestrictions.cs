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

/// <summary>产奶者指定：谁可以直接吸奶、谁可以对我挤奶（名单默认子女+伴侣）、谁可以使用我产出的奶/奶制品（默认仅自己）。筛选用：全部/殖民者/囚犯；表格列：姓名、关系、吸奶、挤奶、使用制品。</summary>
public class Window_ProducerRestrictions : Window
{
    private readonly Pawn producer;
    private readonly CompEquallyMilkable comp;
    private const float EntryHeight = 28f;
    private const float HeaderHeight = 24f;
    private const float CheckboxColWidth = 28f;
    private const float IconSize = 24f;
    private const float ScrollGap = 16f;
    /// <summary>窄于此时「允许自己」改为两行布局（第一行挤奶+吃奶，第二行泄精+塞住）。</summary>
    private const float AllowSelfNarrowThreshold = 520f;

    /// <summary>筛选：控制表格中显示谁（全部 / 仅殖民者 / 仅囚犯）。
    /// 当前 UI 不再提供筛选器（你要求删除筛选框），因此默认始终为 All。</summary>
    private ProducerRestrictionFilter filter = ProducerRestrictionFilter.All;

    private Vector2 tableScrollPosition;

    private static readonly List<Pawn> tmpFiltered = new();

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

    /// <summary>是否在「可以吸奶」上显示为允许。名单非空时看名单；空时看 defaultBreastfeedersWhenEmpty（若为 null 则现场取默认）。</summary>
    private bool IsAllowedBreastfeed(Pawn p, ISet<Pawn> defaultBreastfeedersWhenEmpty)
    {
        if (comp?.allowedBreastfeeders == null) return false;
        if (comp.allowedBreastfeeders.Count > 0) return comp.allowedBreastfeeders.Contains(p);
        if (defaultBreastfeedersWhenEmpty != null) return defaultBreastfeedersWhenEmpty.Contains(p);
        return MilkPermissionExtensions.GetDefaultSucklers(producer).Contains(p);
    }

    private void SetAllowedBreastfeed(Pawn p, bool allow)
    {
        if (comp == null) return;
        SetAllowedWithDefaults(comp.allowedBreastfeeders, p, allow, MilkPermissionExtensions.GetDefaultSucklers(producer));
    }

    /// <summary>是否在「可以挤奶」上显示为允许。名单非空时看名单；空时看 defaultMilkersWhenEmpty（若为 null 则现场取默认）。</summary>
    private bool IsAllowedMilking(Pawn p, ISet<Pawn> defaultMilkersWhenEmpty)
    {
        if (comp?.allowedMilkers == null) return false;
        if (comp.allowedMilkers.Count > 0) return comp.allowedMilkers.Contains(p);
        if (defaultMilkersWhenEmpty != null) return defaultMilkersWhenEmpty.Contains(p);
        return MilkPermissionExtensions.GetDefaultSucklers(producer).Contains(p);
    }

    private void SetAllowedMilking(Pawn p, bool allow)
    {
        if (comp == null) return;
        SetAllowedWithDefaults(comp.allowedMilkers, p, allow, MilkPermissionExtensions.GetDefaultSucklers(producer));
    }

    private void SetAllowedWithDefaults(List<Pawn> allowedList, Pawn p, bool allow, IEnumerable<Pawn> defaults)
    {
        if (comp == null || allowedList == null || p == null) return;
        comp.EnsureAllowedLists();

        if (allow)
        {
            if (!allowedList.Contains(p))
            {
                allowedList.Add(p);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            return;
        }

        // 取消勾选：如果当前名单为空，就先恢复默认名单，再尝试移除 p
        if (allowedList.Count == 0)
        {
            foreach (Pawn x in defaults)
            {
                if (x == null || x.Destroyed) continue;
                if (!(producer.MapHeld == null || x.MapHeld == null || x.MapHeld == producer.MapHeld)) continue;
                if (!allowedList.Contains(x))
                    allowedList.Add(x);
            }
        }

        if (allowedList.Remove(p))
            SoundDefOf.Click.PlayOneShotOnCamera();
    }

    private bool IsAllowedConsumer(Pawn p) => comp?.allowedConsumers != null && comp.allowedConsumers.Contains(p);

    private void SetAllowedConsumer(Pawn p, bool allow)
    {
        if (comp == null) return;
        comp.EnsureAllowedLists();
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

        // 你要求删除筛选框：此处不再绘制筛选 Tab。
        // 留出少量垂直间距，避免表头贴得太近。
        y += 6f;

        bool showBreastfeedColumn = producer.gender == Gender.Female;
        bool showMilkingColumn = showBreastfeedColumn;
        bool forCumProducts = producer.gender == Gender.Male && !producer.IsLactating();
        bool showConsumersColumn = forCumProducts
            ? MilkCumSettings.IsProducerRestrictionConsumersEffectiveForCumProducts()
            : MilkCumSettings.IsProducerRestrictionConsumersEffectiveForMilkProducts();

        comp.EnsureAllowedLists();

        // 名单为空时缓存默认吸奶名单，避免每行重复计算
        ISet<Pawn> cachedDefaultBreastfeeders = null;
        ISet<Pawn> cachedDefaultMilkers = null;
        if (showBreastfeedColumn && comp.allowedBreastfeeders.Count == 0)
            cachedDefaultBreastfeeders = MilkPermissionExtensions.GetDefaultSucklers(producer).ToHashSet();
        if (showMilkingColumn && comp.allowedMilkers.Count == 0)
            cachedDefaultMilkers = MilkPermissionExtensions.GetDefaultSucklers(producer).ToHashSet();

        // 表头：姓名 | 关系 | 吸奶 | 挤奶 | 可以使用制品
        float nameW = Mathf.Max(80f, inRect.width * 0.35f);
        float relW = Mathf.Min(80f, inRect.width * 0.2f);
        float breastfeedW = showBreastfeedColumn ? CheckboxColWidth + 4f : 0f;
        float milkingW = showMilkingColumn ? CheckboxColWidth + 4f : 0f;
        float consumeW = showConsumersColumn ? CheckboxColWidth + 4f : 0f;
        float rest = inRect.width - nameW - relW - breastfeedW - milkingW - consumeW;
        // 给复选/选项列预留空间：剩余宽度优先补到关系列（而不是姓名列），避免“姓名列占太宽导致右侧挤压/空白很多”。
        if (rest < 0f) { relW += rest; rest = 0f; }
        else { relW += rest; }

        GUI.color = Color.gray;
        Rect hName = new Rect(inRect.x, y, nameW, HeaderHeight);
        Widgets.Label(hName, "EM.RestrictionsTableHeaderName".Translate());
        Rect hRel = new Rect(hName.xMax, y, relW, HeaderHeight);
        Widgets.Label(hRel, "EM.RestrictionsTableHeaderRelation".Translate());
        float cx = hRel.xMax;
        if (showBreastfeedColumn)
        {
            Rect hBreastfeed = new Rect(cx, y, breastfeedW, HeaderHeight);
            Widgets.Label(hBreastfeed, "EM.RestrictionsTableHeaderBreastfeed".Translate());
            TooltipHandler.TipRegion(hBreastfeed, "EM.RestrictionsTableHeaderBreastfeedTip".Translate());
            cx = hBreastfeed.xMax;
        }
        if (showMilkingColumn)
        {
            Rect hMilking = new Rect(cx, y, milkingW, HeaderHeight);
            Widgets.Label(hMilking, "EM.RestrictionsTableHeaderMilking".Translate());
            TooltipHandler.TipRegion(hMilking, "EM.RestrictionsTableHeaderMilkingTip".Translate());
            cx = hMilking.xMax;
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
        float bW = showBreastfeedColumn ? CheckboxColWidth + 4f : 0f;
        float mW = showMilkingColumn ? CheckboxColWidth + 4f : 0f;
        float cW = showConsumersColumn ? CheckboxColWidth + 4f : 0f;
        float remainder = bodyWidth - nW - rW - bW - mW - cW;
        // 同 header：剩余宽度优先补到关系列，减少姓名列无效空白。
        if (remainder < 0f) rW += remainder;
        else rW += remainder;

        // 自适应滚动框高度：
        // 1) 人少时：滚动框高度 = 实际行高总和，避免框下多余空白。
        // 2) 人多时：滚动框高度 = 可显示的最大行数 * 行高，让它占满可用空间并可滚动。
        float contentHeight = pawns.Count * EntryHeight;
        float availableHeight = Mathf.Max(0f, inRect.yMax - y);
        float maxVisibleRows = Mathf.Floor(availableHeight / EntryHeight);
        float viewportRowsHeight = Mathf.Max(EntryHeight, maxVisibleRows * EntryHeight);
        float scrollOutHeight = contentHeight <= viewportRowsHeight ? contentHeight : viewportRowsHeight;
        // 防御：可用高度过小也保证至少能显示一行
        scrollOutHeight = Mathf.Max(EntryHeight, scrollOutHeight);
        Rect scrollOutRect = new Rect(inRect.x, y, inRect.width, scrollOutHeight);
        Rect scrollViewRect = new Rect(0f, 0f, bodyWidth, contentHeight);
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
            if (showBreastfeedColumn)
            {
                Rect rBreastfeed = new Rect(checkX, rowY + (EntryHeight - CheckboxColWidth) / 2f, CheckboxColWidth, CheckboxColWidth);
                bool breastfeedChecked = IsAllowedBreastfeed(p, cachedDefaultBreastfeeders);
                bool oldBreastfeed = breastfeedChecked;
                Widgets.Checkbox(rBreastfeed.position, ref breastfeedChecked, CheckboxColWidth);
                if (breastfeedChecked != oldBreastfeed)
                    SetAllowedBreastfeed(p, breastfeedChecked);
                checkX = rBreastfeed.xMax;
            }
            if (showMilkingColumn)
            {
                Rect rMilking = new Rect(checkX, rowY + (EntryHeight - CheckboxColWidth) / 2f, CheckboxColWidth, CheckboxColWidth);
                bool milkingChecked = IsAllowedMilking(p, cachedDefaultMilkers);
                bool oldMilking = milkingChecked;
                Widgets.Checkbox(rMilking.position, ref milkingChecked, CheckboxColWidth);
                if (milkingChecked != oldMilking)
                    SetAllowedMilking(p, milkingChecked);
                checkX = rMilking.xMax;
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

        // 窄屏溢出修复：每个 checkbox 在宽度不足时自动换行，确保不会画到窗口右侧之外。
        bool narrow = width < AllowSelfNarrowThreshold;
        float rowY = y;
        float cx = x;
        float maxX = x + width;

        // 用 narrow 保留“先画完挤奶/吃奶，再进入第二行”的视觉结构
        void EnsureRowStartIfNarrow(bool condition)
        {
            if (!condition) return;
            rowY += rowHeight + 4f;
            cx = x;
        }

        void DrawCheckbox(string labelKey, string tipKey, float desiredWidth, System.Func<bool> getter, System.Action<bool> setter)
        {
            if (desiredWidth <= 0f) return;
            // 宽度不足时换行
            if (cx + desiredWidth > maxX && cx > x)
            {
                rowY += rowHeight + 4f;
                cx = x;
            }

            float w = Mathf.Min(desiredWidth, maxX - cx);
            if (w <= 0.01f) return;

            Rect r = new Rect(cx, rowY, w, rowHeight);
            bool val = getter();
            bool oldVal = val;
            Widgets.CheckboxLabeled(r, labelKey.Translate(), ref val);
            TooltipHandler.TipRegion(r, tipKey.Translate());
            if (val != oldVal) setter(val);
            cx = r.xMax + gap;
        }

        // 第一行：挤奶、吃奶
        if (comp?.MilkSettings != null)
        {
            DrawCheckbox(
                "EM.RestrictionsAllowSelf_Milking",
                "EM.RestrictionsAllowSelf_MilkingTip",
                160f,
                producer.AllowMilkingSelf,
                v => producer.SetAllowMilkingSelf(v));

            DrawCheckbox(
                "EM.RestrictionsAllowSelf_Consume",
                "EM.RestrictionsAllowSelf_ConsumeTip",
                140f,
                producer.AllowSelfConsumeProducts,
                v => producer.SetAllowSelfConsumeProducts(v));
        }

        EnsureRowStartIfNarrow(narrow);

        // 第二行：泄精（有 Comp 即可）；塞住仅当 canSeal()，避免 IsSealed getter 在无法塞住时恒为 false 导致勾选状态不同步。
        if (sealComp != null)
        {
            DrawCheckbox(
                "EM.Milk_AllowDeflate",
                "EM.Milk_AllowDeflateTip",
                120f,
                sealComp.CanDeflate,
                v => sealComp.SetCanDeflate(v));

            if (sealComp.canSeal())
            {
                DrawCheckbox(
                    "EM.Milk_SealCum",
                    "EM.Milk_SealCumTip",
                    100f,
                    sealComp.IsSealed,
                    v => sealComp.SetSealed(v));
            }
        }

        y = rowY + rowHeight + 8f;
        Widgets.DrawLineHorizontal(x, y, width);
        y += 6f;
    }
}
