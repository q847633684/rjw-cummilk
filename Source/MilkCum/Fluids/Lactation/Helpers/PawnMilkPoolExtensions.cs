using System;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Shared.Data;
using MilkCum.RJW;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 乳池与流速：池条目、容量、MilkDef、泌乳 Hediff 访问、流速倍率（RJW/基因/乳腺炎）、压奶满度、身体部位、距离等。
/// 设计：泌乳乳房条目的经济快照以 <see cref="RjwBreastPoolSnapshot"/> 为 SSOT；左右真实池与 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/> / <see cref="FluidPoolEntry"/> 对齐。不乘 BodySize。
/// </summary>
public static class PawnMilkPoolExtensions
{
    /// <summary>左侧各池基容量之和（组织适应前因子，由 <see cref="GetBreastCapacityFactors"/>）。</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右侧各池基容量之和。</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>左/右汇总基容量因子（各侧乳池 <see cref="FluidPoolEntry.Capacity"/> 之和，适应前由侧行加总）。</summary>
    private static void GetBreastCapacityFactors(Pawn pawn, out float leftFactor, out float rightFactor)
    {
        leftFactor = 0f;
        rightFactor = 0f;
        if (pawn == null) return;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return;
        try
        {
            var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
            if (rows.Count == 0) return;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.IsLeft) leftFactor += r.BaseCapacity;
                else if (RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(r.BreastHediff?.Part)) rightFactor += r.BaseCapacity;
            }
        }
        catch (Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(GetBreastCapacityFactors), ex);
        }
    }

    /// <summary>每条侧池一行：<see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/>；当前模型按乳房子部位一池，不再做左右对耦合。</summary>
    public static List<FluidPoolEntry> GetBreastPoolEntries(this Pawn pawn)
    {
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return new List<FluidPoolEntry>();
        return BuildBreastPoolEntriesFromSideRows(pawn, RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn));
    }

    /// <summary>由已算好的侧行构建池条目（与 <see cref="GetBreastPoolEntries"/> 同逻辑）；供 <see cref="CompEquallyMilkable"/> 单次算行、复用行与条目。</summary>
    internal static List<FluidPoolEntry> BuildBreastPoolEntriesFromSideRows(Pawn pawn, List<RjwBreastPoolSideRow> rows)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null || rows == null || rows.Count == 0) return result;
        try
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                BodyPartRecord part = r.BreastHediff?.Part;
                result.Add(new FluidPoolEntry(
                    r.PoolKey,
                    r.BaseCapacity,
                    r.FlowMultiplier,
                    r.IsLeft,
                    r.PoolIndex,
                    part));
            }
            ApplyCapacityAdaptationToEntries(pawn, result);
        }
        catch (Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(BuildBreastPoolEntriesFromSideRows), ex);
            result.Clear();
        }
        return result;
    }

    /// <summary>将 Comp 上的组织适应增量按各侧体基容量比例摊入 Capacity，使 sum(Capacity)≈maxFullness，与进水、回缩、GetPoolBaseTotal、UI 一致。</summary>
    private static void ApplyCapacityAdaptationToEntries(Pawn pawn, List<FluidPoolEntry> result)
    {
        if (result == null || result.Count == 0) return;
        float sumCap = 0f;
        for (int i = 0; i < result.Count; i++)
            sumCap += result[i].Capacity;
        if (sumCap < 0.01f) return;
        var milkComp = pawn?.CompEquallyMilkable();
        float adapt = milkComp != null ? milkComp.CapacityAdaptation : 0f;
        if (adapt <= PoolModelConstants.Epsilon) return;
        for (int i = 0; i < result.Count; i++)
        {
            var e = result[i];
            float add = adapt * (e.Capacity / sumCap);
            result[i] = new FluidPoolEntry(e.Key, e.Capacity + add, e.FlowMultiplier, e.IsLeft, e.PoolIndex, e.SourcePart);
        }
    }

    public static ThingDef MilkDef(this Pawn pawn) => MilkCumSettings.GetMilkProductDef(pawn);
    public static float MilkAmount(this Pawn pawn) => MilkCumSettings.GetMilkAmount(pawn);
    public static float MilkMarketValue(this Pawn pawn) => pawn.MilkDef()?.BaseMarketValue ?? 0;
    public static bool MilkTypeCanBreastfeed(this Pawn mom) => MilkCumSettings.MilkTypeCanBreastfeed(mom);
    public static bool CanBreastfeedEver(this Pawn mom, Pawn baby) => MilkCumSettings.CanBreastfeedEver(mom, baby);
    public static HediffComp_EqualMilkingLactating LactatingHediffComp(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating)?.TryGetComp<HediffComp_EqualMilkingLactating>();
    public static Hediff LactatingHediff(this Pawn pawn) => pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating);
    public static HediffWithComps_MilkCumLactating LactatingHediffWithComps(this Pawn pawn) => pawn.LactatingHediff() as HediffWithComps_MilkCumLactating;

    /// <summary>乳腺炎/瘀积等对流速的惩罚：返回 1 减去有关 Def 的 severity×权重，有下限。part 非空时仅匹配该部位。</summary>
    public static float GetMilkFlowMultiplierFromConditions(this Pawn pawn, BodyPartRecord part = null)
    {
        if (pawn?.health?.hediffSet == null) return 1f;
        float eff = 0f;
        void Acc(HediffDef def, float weight)
        {
            if (def == null) return;
            Hediff h = part == null
                ? pawn.health.hediffSet.GetFirstHediffOfDef(def)
                : pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.def == def && x.Part == part);
            if (h != null) eff = Mathf.Max(eff, h.Severity * weight);
        }
        Acc(MilkCumDefOf.EM_LactationalMilkStasis, 0.32f);
        Acc(MilkCumDefOf.EM_Mastitis, 0.5f);
        Acc(MilkCumDefOf.EM_BreastAbscess, 0.62f);
        if (eff <= 0f) return 1f;
        return Mathf.Clamp(1f - eff, 0.35f, 1f);
    }

    /// <summary>稳定池键对应的 RJW 乳房 <see cref="Hediff.Part"/>。</summary>
    public static BodyPartRecord GetPartForPoolKey(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return null;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].PoolKey == poolKey)
                return rows[i].BreastHediff?.Part;
        }
        return null;
    }

    /// <summary>稳定池键上的状态系数（乳腺炎/瘀积等）。</summary>
    public static float GetConditionsForPoolKey(this Pawn pawn, string poolKey) =>
        pawn == null || string.IsNullOrEmpty(poolKey)
            ? 1f
            : pawn.GetMilkFlowMultiplierFromConditions(pawn.GetPartForPoolKey(poolKey));

    /// <summary>给定乳房 hediff，返回其某一侧池键（优先左）。</summary>
    public static string GetPoolKeyForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        if (pawn == null || breastHediff == null) return null;
        if (!RjwBreastPoolEconomy.IsBreastHediffForPool(breastHediff)) return null;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        string any = null;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].BreastHediff != breastHediff) continue;
            if (rows[i].IsLeft) return rows[i].PoolKey;
            any = rows[i].PoolKey;
        }
        return any;
    }

    /// <summary>左右池各自的日流速与 RJW 倍率（读 <see cref="CompEquallyMilkable"/> 缓存）。</summary>
    public static (float flowLeft, float flowRight, float multLeft, float multRight) GetFlowPerDayForBreastSides(this Pawn pawn, string leftKey, string rightKey)
    {
        if (pawn == null || pawn.LactatingHediffComp() == null) return (0f, 0f, 0f, 0f);
        var milkComp = pawn.CompEquallyMilkable();
        var entries = milkComp?.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        float multL = 0f, multR = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Key == leftKey) multL = e.FlowMultiplier;
            if (e.Key == rightKey) multR = e.FlowMultiplier;
        }
        if (milkComp == null || !milkComp.IsCachedFlowValid())
            return (0f, 0f, multL, multR);
        float fl = string.IsNullOrEmpty(leftKey) ? 0f : milkComp.GetFlowPerDayForKeyCached(leftKey);
        float fr = string.IsNullOrEmpty(rightKey) ? 0f : milkComp.GetFlowPerDayForKeyCached(rightKey);
        return (fl, fr, multL, multR);
    }

    /// <summary>该池键满度 / 撑大上限 的压力因子。</summary>
    public static float GetPressureFactorForPool(this Pawn pawn, string poolKey)
    {
        var milkComp = pawn?.CompEquallyMilkable();
        if (milkComp == null || string.IsNullOrEmpty(poolKey)) return 1f;
        var entries = milkComp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        if (entries.Count == 0) return 1f;
        var e = entries.FirstOrDefault(x => x.Key == poolKey);
        if (string.IsNullOrEmpty(e.Key)) return 1f;
        float stretch = e.Capacity * PoolModelConstants.StretchCapFactor;
        float fullE = milkComp.GetFullnessForKey(poolKey);
        float pressure = MilkCumSettings.enablePressureFactor
            ? MilkCumSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
            : (fullE >= stretch ? 0f : 1f);
        var lact = pawn.LactatingHediffComp();
        float resL = lact?.CurrentLactationAmount ?? 0f;
        float resI = lact?.GetInflammationForKey(poolKey) ?? 0f;
        MilkCumSettings.ApplyOverflowResidualFlow(ref pressure, fullE, stretch, resL, resI);
        return pressure;
    }

    /// <summary>该乳房 Hediff 对应的各侧真实池（若有合成左右则两行）。</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        var list = new List<(string, float, float, bool)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || breastHediff == null) return list;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        var entries = comp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.BreastHediff != breastHediff) continue;
            float cap = TryCapacityForKey(entries, r.PoolKey, r.BaseCapacity);
            float f = comp.GetFullnessForKey(r.PoolKey);
            list.Add((r.PoolKey, f, cap, r.IsLeft));
        }
        return list;
    }

    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForPoolKey(this Pawn pawn, string poolKey)
    {
        var list = new List<(string, float, float, bool)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || string.IsNullOrEmpty(poolKey)) return list;
        var entries = comp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Key != poolKey) continue;
            list.Add((poolKey, comp.GetFullnessForKey(poolKey), e.Capacity, e.IsLeft));
            break;
        }
        return list;
    }

    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastPart(this Pawn pawn, BodyPartRecord part)
    {
        var list = new List<(string, float, float, bool)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || part == null) return list;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        var entries = comp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.BreastHediff?.Part != part) continue;
            float cap = TryCapacityForKey(entries, r.PoolKey, r.BaseCapacity);
            list.Add((r.PoolKey, comp.GetFullnessForKey(r.PoolKey), cap, r.IsLeft));
        }
        return list;
    }

    static float TryCapacityForKey(List<FluidPoolEntry> entries, string key, float fallback)
    {
        if (entries == null) return fallback;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Key == key) return entries[i].Capacity;
        return fallback;
    }

    /// <summary>获取「乳房/胸部」身体部位，用于挂乳房类 hediff。优先 Breast，否则 Chest（RJW），再否则 Torso。无合适部位时返回 null</summary>
    public static BodyPartRecord GetBreastOrChestPart(this Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return null;
        var parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null);
        BodyPartRecord breast = null, chest = null, torso = null;
        foreach (var p in parts)
        {
            if (p?.def?.defName == null) continue;
            var dn = p.def.defName;
            if (dn.IndexOf("Breast", System.StringComparison.OrdinalIgnoreCase) >= 0) { breast = p; break; }
            if (dn == "Chest") chest = p;
            if (dn == "Torso") torso = p;
        }
        return breast ?? chest ?? torso;
    }

    /// <summary>获取生殖器部位（RJW Genital_Helper），失败时回退 Genitals/Anus</summary>
    public static BodyPartRecord GetGenitalsPart(this Pawn pawn)
    {
        if (pawn == null) return null;
        try
        {
            var bpr = Genital_Helper.get_genitalsBPR(pawn);
            if (bpr != null) return bpr;
        }
        catch (Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(GetGenitalsPart), ex);
        }
        if (pawn.health?.hediffSet == null) return null;
        var parts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null);
        BodyPartRecord genitals = null, anus = null;
        foreach (var p in parts)
        {
            if (p?.def?.defName == null) continue;
            if (p.def.defName == "Genitals") genitals = p;
            if (p.def.defName == "Anus") anus = p;
        }
        return genitals ?? anus;
    }

    public static float DistanceTo(this Pawn pawn, Pawn other)
    {
        if (pawn.Map != other.Map || pawn.Map == null || other.Map == null) return float.MaxValue;
        return pawn.Position.DistanceTo(other.Position);
    }

    public static bool IsAdultFemaleAnimalOfColony(this Pawn pawn)
    {
        return pawn.Faction == Faction.OfPlayer
            && pawn.IsNormalAnimal()
            && !(pawn.LactatingHediff()?.Severity >= 1)
            && pawn.gender != Gender.Male
            && pawn.ageTracker.CurLifeStage?.reproductive == true;
    }
}
