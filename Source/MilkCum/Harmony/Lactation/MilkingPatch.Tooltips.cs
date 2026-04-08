using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Harmony;

internal static class BreastPoolTooltipHelper
{
    private const string Tab = "  ";

    public static string BuildBreastPoolBlock(string sectionLabel, List<(string key, float fullness, float capacity, bool isLeft)> entries, Pawn pawn, bool showSiblingPairHint = false)
    {
        if ((entries?.Count ?? 0) == 0 || pawn == null) return "";
        var lactComp = pawn.LactatingHediffComp();
        var milkComp = pawn.CompEquallyMilkable();
        if (lactComp == null) return "";

        var entList = milkComp?.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();

        float lf = 0f, lc = 0f, lflow = 0f;
        float rf = 0f, rc = 0f, rflow = 0f;
        float af = 0f, ac = 0f, aflow = 0f;
        string skL = null, skR = null, skA = null;
        bool flowOk = milkComp != null && milkComp.IsCachedFlowValid();
        int keyCount = 0;
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.key)) continue;
            keyCount++;
            BodyPartRecord part = pawn.GetPartForPoolKey(e.key);
            float fl = flowOk ? milkComp.GetFlowPerDayForKeyCached(e.key) : 0f;
            bool virtualRightSite = false;
            if (entList != null)
            {
                for (int j = 0; j < entList.Count; j++)
                {
                    if (entList[j].Key != e.key) continue;
                    virtualRightSite = entList[j].Site == FluidSiteKind.BreastRight;
                    break;
                }
            }
            if (e.isLeft)
            {
                lf += e.fullness;
                lc += e.capacity;
                lflow += fl;
                skL ??= e.key;
            }
            else if (RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(part) || virtualRightSite)
            {
                rf += e.fullness;
                rc += e.capacity;
                rflow += fl;
                skR ??= e.key;
            }
            else
            {
                af += e.fullness;
                ac += e.capacity;
                aflow += fl;
                skA ??= e.key;
            }
        }

        if (keyCount == 0) return "";
        bool hasCapL = lc >= 0.001f;
        bool hasCapR = rc >= 0.001f;
        bool hasCapA = ac >= 0.001f;
        bool showL = skL != null || hasCapL || lf >= 0.001f || (flowOk && lflow >= 0.001f);
        bool showR = skR != null || hasCapR || rf >= 0.001f || (flowOk && rflow >= 0.001f);
        bool showA = skA != null || hasCapA || af >= 0.001f || (flowOk && aflow >= 0.001f);
        if (!showL && !showR && !showA) return "";
        var b = lactComp.GetFlowPerDayBreakdown();
        float totalMilk = lf + rf + af;
        float totalBaseCap = Mathf.Max(0.001f, lc + rc + ac);
        float totalStretchCap = totalBaseCap * PoolModelConstants.StretchCapFactor;
        float flowTotal = lflow + rflow + aflow;
        string totalPercent = totalBaseCap >= 0.001f ? (totalMilk / totalBaseCap).ToStringPercent() : "0%";

        bool singleLeafRow = keyCount == 1;
        string singleRowTitle = "EM.PoolBreastSingleRowDetail".Translate(sectionLabel);
        string anySk = skL ?? skR ?? skA;
        BodyPartRecord samplePart = string.IsNullOrEmpty(anySk) ? null : pawn.GetPartForPoolKey(anySk);
        bool chestBackedSinglePool = singleLeafRow && (samplePart == null || samplePart.def?.defName == "Chest");

        string TitleForAggregatedColumn(bool leftCol, bool rightCol, bool ambigCol)
        {
            if (!singleLeafRow)
            {
                if (leftCol) return "EM.PoolLeftBreast".Translate();
                if (rightCol) return "EM.PoolRightBreast".Translate();
                return "EM.PoolBreastAmbiguousSide".Translate();
            }
            if (chestBackedSinglePool)
                return "EM.PoolBreastChestSinglePoolTitle".Translate(sectionLabel);
            return singleRowTitle;
        }

        var sb = new StringBuilder();
        sb.AppendLine("EM.PoolBreastSectionHeader".Translate(sectionLabel));
        sb.AppendLine();
        if (showSiblingPairHint)
        {
            sb.AppendLine("EM.PoolBreastTooltipSiblingPairHint".Translate());
            sb.AppendLine();
        }

        if (!singleLeafRow)
        {
            sb.AppendLine("EM.PoolSectionStorage".Translate());
            sb.Append(Tab).AppendLine("EM.PoolBreastTotalMilkLine".Translate(
                totalMilk.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                totalBaseCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
                totalPercent));
            sb.Append(Tab).AppendLine("EM.PoolBreastStretchCapLine".Translate(totalStretchCap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
            sb.AppendLine();
            sb.AppendLine("EM.PoolSectionFlow".Translate());
            sb.Append(Tab).AppendLine("EM.PoolBreastTotalFlowLine".Translate(flowTotal.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
            sb.AppendLine();
        }

        if (showL)
            AppendAggregatedBreastSide(sb, lactComp, b, TitleForAggregatedColumn(true, false, false), lf, lc, lflow, FlowMultForBreastKey(entList, skL), skL, pawn);

        if (showR)
        {
            sb.AppendLine();
            AppendAggregatedBreastSide(sb, lactComp, b, TitleForAggregatedColumn(false, true, false), rf, rc, rflow, FlowMultForBreastKey(entList, skR), skR, pawn);
        }

        if (showA)
        {
            sb.AppendLine();
            AppendAggregatedBreastSide(sb, lactComp, b, TitleForAggregatedColumn(false, false, true), af, ac, aflow, FlowMultForBreastKey(entList, skA), skA, pawn);
        }

        return sb.ToString();
    }

    static float FlowMultForBreastKey(List<FluidPoolEntry> list, string k)
    {
        if (string.IsNullOrEmpty(k) || list == null) return 0f;
        for (int i = 0; i < list.Count; i++)
            if (list[i].Key == k)
                return list[i].FlowMultiplier;
        return 0f;
    }

    static void AppendAggregatedBreastSide(
        StringBuilder sb,
        HediffComp_EqualMilkingLactating lactComp,
        HediffComp_EqualMilkingLactating.FlowBreakdown b,
        string sideTitle,
        float full,
        float cap,
        float flow,
        float mult,
        string poolKey,
        Pawn pawn)
    {
        sb.AppendLine(sideTitle);
        string pct = cap >= 0.001f ? (full / cap).ToStringPercent() : "0%";
        float stretch = cap * PoolModelConstants.StretchCapFactor;
        sb.Append(Tab).AppendLine("EM.PoolBreastSideMilkLine".Translate(
            full.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            cap.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute),
            pct));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideFlowLine".Translate(flow.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        sb.Append(Tab).AppendLine("EM.PoolBreastSideCapMax".Translate(stretch.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
        if (!Prefs.DevMode || string.IsNullOrEmpty(poolKey)) return;
        float letdown = lactComp.GetLetdownReflexFlowMultiplier(poolKey);
        float pressure = pawn.GetPressureFactorForPool(poolKey);
        float conditions = pawn.GetConditionsForPoolKey(poolKey);
        var factorLines = HediffComp_EqualMilkingLactating.BuildBreastEfficiencyFactorLinesForDevMode(b, mult, letdown, pressure, conditions);
        sb.Append(Tab).AppendLine("EM.PoolBreastDevModeOnly".Translate());
        sb.Append(Tab).AppendLine("EM.PoolBreastMechanismLabel".Translate());
        foreach (string line in factorLines)
            sb.Append(Tab).Append(Tab).AppendLine(line);
    }
}

public static class HealthCardUtility_GetTooltip_BreastPool_Patch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        MethodBase m = AccessTools.Method(typeof(HealthCardUtility), "GetTooltip", new[] { typeof(IEnumerable<Hediff>), typeof(Pawn), typeof(BodyPartRecord) });
        if (m == null)
        {
            Log.Message("[MilkCum] HealthCardUtility.GetTooltip(hediffs,pawn,part) not found; breast pool tooltip append skipped.");
            return;
        }
        harmony.Patch(m, postfix: new HarmonyMethod(typeof(HealthCardUtility_GetTooltip_BreastPool_Patch), nameof(Postfix)));
    }

    public static void Postfix(IEnumerable<Hediff> diffs, Pawn pawn, BodyPartRecord part, ref string __result)
    {
        if (pawn == null || diffs == null) return;
        if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
        try
        {
            foreach (Hediff h in diffs)
            {
                if (h == null || h is HediffWithComps_MilkCumLactating) continue;
                if (!IsBreastLikeHediffForPoolAppend(h)) continue;
                string sectionLabel = h.LabelCap;
                string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
                if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) continue;
                var entries = pawn.GetPoolEntriesForBreastHediff(h, out bool sibHint);
                if (entries.Count == 0) continue;
                string block = BreastPoolTooltipHelper.BuildBreastPoolBlock(sectionLabel, entries, pawn, sibHint);
                if (!string.IsNullOrEmpty(block))
                    __result = (__result ?? "") + "\n" + block;
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HealthCardUtility.GetTooltip breast pool append: {ex.Message}");
        }
    }

    static bool IsBreastLikeHediffForPoolAppend(Hediff h)
    {
        if (h?.def is HediffDef_SexPart sp && sp.genitalFamily == GenitalFamily.Breasts) return true;
        string dn = h?.Part?.def?.defName;
        return !string.IsNullOrEmpty(dn) && dn.IndexOf("Breast", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

[HarmonyPatch(typeof(HediffComp_SexPart), "get_CompTipStringExtra")]
public static class HediffComp_SexPart_CompTipStringExtra_Patch
{
    [HarmonyPostfix]
    public static void Postfix(HediffComp_SexPart __instance, ref string __result)
    {
        if (__instance?.parent == null) return;
        Hediff parent = __instance.parent;
        if (parent?.pawn == null) return;
        Pawn pawn = parent.pawn;
        try
        {
            GenitalFamily gf = __instance.Def.genitalFamily;
            if (gf == GenitalFamily.Breasts)
            {
                if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
                string sectionLabel = parent.LabelCap;
                string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
                if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) return;
                var entries = pawn.GetPoolEntriesForBreastHediff(parent, out bool sibHint);
                if (entries.Count == 0) return;
                string block = BreastPoolTooltipHelper.BuildBreastPoolBlock(sectionLabel, entries, pawn, sibHint);
                if (!string.IsNullOrEmpty(block))
                    __result = (__result ?? "") + "\n" + block;
                return;
            }

            if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return;
            if (gf != GenitalFamily.Penis && gf != GenitalFamily.MaleOvipositor) return;
            if (__instance.Def.genitalTags == null || !__instance.Def.genitalTags.Contains(GenitalTag.CanPenetrate)) return;
            if (parent is not ISexPartHediff sp || !RjwSemenPoolEconomy.IsPenisLikeSemenPart(sp)) return;

            string semenHeader = "EM.HealthTabVirtualSemenPoolHeader".Translate();
            if (!string.IsNullOrEmpty(__result) && __result.Contains(semenHeader)) return;

            var rows = pawn.CompVirtualSemenPool().GetSemenPoolDisplayRows(pawn);
            if (rows.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append("\n");
            sb.AppendLine(semenHeader);
            foreach ((FluidSiteKind site, float current, float capacity) in rows)
            {
                string side = site == FluidSiteKind.TesticleLeft
                    ? "EM.VirtualSemenPoolLeft".Translate()
                    : "EM.VirtualSemenPoolRight".Translate();
                sb.AppendLine($"{side}: {current.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)} / {capacity.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)}");
            }

            __result = (__result ?? "") + sb.ToString();
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HediffComp_SexPart CompTipStringExtra pool append: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(HediffWithComps), "get_TipStringExtra")]
public static class HediffWithComps_TipStringExtra_BreastPoolAppend_Patch
{
    [HarmonyPostfix]
    public static void Postfix(HediffWithComps __instance, ref string __result)
    {
        if (__instance == null || __instance is HediffWithComps_MilkCumLactating) return;
        Pawn pawn = __instance.pawn;
        if (pawn == null) return;
        try
        {
            if (pawn.CompEquallyMilkable() == null || !pawn.IsLactating()) return;
            bool isBreastLikeRow = false;
            if (__instance.def is HediffDef_SexPart sp && sp.genitalFamily == GenitalFamily.Breasts)
                isBreastLikeRow = true;
            else
                isBreastLikeRow = __instance.Part?.def?.defName?.IndexOf("Breast", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isBreastLikeRow) return;

            string sectionLabel = __instance.LabelCap;
            string blockHeader = "EM.PoolBreastSectionHeader".Translate(sectionLabel);
            if (!string.IsNullOrEmpty(__result) && __result.Contains(blockHeader)) return;

            var entries = pawn.GetPoolEntriesForBreastHediff(__instance, out bool sibHint);
            if (entries.Count == 0) return;

            string block = BreastPoolTooltipHelper.BuildBreastPoolBlock(sectionLabel, entries, pawn, sibHint);
            if (!string.IsNullOrEmpty(block))
                __result = (__result ?? "") + "\n" + block;
        }
        catch (Exception ex)
        {
            Log.Warning($"[MilkCum] HediffWithComps TipStringExtra breast pool append: {ex.Message}");
        }
    }
}

public static class Thing_GetStatValue_PartialMilkNutrition_Patch
{
    public static void ApplyIfPossible(HarmonyLib.Harmony harmony)
    {
        MethodBase target = AccessTools.Method(typeof(Thing), "GetStatValue", new[] { typeof(StatDef), typeof(int) })
            ?? AccessTools.Method(typeof(Thing), "GetStatValue", new[] { typeof(StatDef) });
        if (target == null) return;
        var prefix = new HarmonyMethod(typeof(Thing_GetStatValue_PartialMilkNutrition_Patch), nameof(Prefix));
        harmony.Patch(target, prefix: prefix);
    }

    public static bool Prefix(Thing __instance, StatDef stat, ref float __result)
    {
        if (stat != StatDefOf.Nutrition) return true;
        var comp = __instance.TryGetComp<Fluids.Lactation.Comps.CompPartialMilk>();
        if (comp == null || comp.fillAmount <= 0f) return true;
        __result = comp.fillAmount;
        return false;
    }
}
