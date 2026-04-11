using System;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Integration;
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
/// 设计：RJW 解剖行见 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/>；每叶每条乳房 Hediff 两格储奶 <c>稳定基键_L/_R</c>。<see cref="FluidSiteKind.BreastLeft"/>/<see cref="FluidSiteKind.BreastRight"/> 表虚拟左右池汇总。乳房快照基键见 <see cref="RjwBreastPoolSnapshot.PoolKey"/>。不乘 BodySize。
/// </summary>
public static class PawnMilkPoolExtensions
{
    /// <summary>无 RJW 或 pawn 为空时复用；勿 Add/Remove。</summary>
    private static readonly List<FluidPoolEntry> EmptyBreastPoolEntries = new();

    /// <summary>优先 Comp 有效缓存条目，否则 <see cref="CompEquallyMilkable.GetResolvedBreastPoolEntries"/>；无 Comp 时走 <see cref="GetBreastPoolEntries"/>。勿对返回的 Comp 缓存或本静态空列表执行 Add/Remove。</summary>
    public static List<FluidPoolEntry> GetResolvedBreastPoolEntries(this Pawn pawn)
    {
        if (pawn == null) return EmptyBreastPoolEntries;
        var comp = pawn.CompEquallyMilkable();
        if (comp != null)
            return comp.GetResolvedBreastPoolEntries();
        return pawn.GetBreastPoolEntries();
    }

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
        if (!ModIntegrationGates.RjwModActive) return;
        try
        {
            var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
            if (rows.Count == 0) return;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.IsLeft) leftFactor += r.BaseCapacity;
                else if (RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(r.BreastHediff?.Part))
                    rightFactor += r.BaseCapacity;
            }
        }
        catch (Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(GetBreastCapacityFactors), ex);
        }
    }

    /// <summary>虚拟乳池条目：当前固定虚拟左·右，条目键为稳定基键加 <c>_L/_R</c>。</summary>
    public static List<FluidPoolEntry> GetBreastPoolEntries(this Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive) return EmptyBreastPoolEntries;
        var entries = RjwBreastPoolEconomy.BuildVirtualLeftRightBreastPoolEntries(pawn);
        BreastPoolTopologyDiagnostics.MaybeDevWarnAfterEntriesBuilt(pawn, entries);
        return entries;
    }

    /// <summary>与 <see cref="CompEquallyMilkable"/> 池条目缓存一致：按虚拟左·右构建条目。</summary>
    internal static List<FluidPoolEntry> BuildCachedBreastPoolEntries(Pawn pawn) =>
        RjwBreastPoolEconomy.BuildVirtualLeftRightBreastPoolEntries(pawn);

    /// <summary>虚拟左/右拓扑：由侧行生成条目；实现位于 <see cref="RjwBreastPoolEconomy.BuildBreastPoolEntriesFromSideRows"/>。</summary>
    internal static List<FluidPoolEntry> BuildBreastPoolEntriesFromSideRows(Pawn pawn, List<RjwBreastPoolSideRow> rows) =>
        RjwBreastPoolEconomy.BuildBreastPoolEntriesFromSideRows(pawn, rows);

    /// <summary>将 Comp 上的组织适应增量按各池基容量比例摊入 Capacity，使 sum(Capacity)≈maxFullness，与进水、回缩、GetPoolBaseTotal、UI 一致。</summary>
    internal static void ApplyCapacityAdaptationToBreastEntries(Pawn pawn, List<FluidPoolEntry> result)
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
            result[i] = new FluidPoolEntry(e.Key, e.Site, e.Capacity + add, e.FlowMultiplier, e.IsLeft, e.PoolIndex, e.SourcePart);
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

    /// <summary>储奶键或稳定基键（无 <c>_L/_R</c>）解析 <see cref="Hediff.Part"/>（乳腺炎等部位倍率）。</summary>
    public static BodyPartRecord GetPartForPoolKey(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return null;
        var entries = pawn.GetResolvedBreastPoolEntries();
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Key == poolKey)
                return entries[i].SourcePart;
        if (!RjwBreastPoolEconomy.TryStripVirtualBreastStorageSuffix(poolKey, out _))
        {
            string tryL = poolKey + RjwBreastPoolEconomy.VirtualBreastLeftStorageSuffix;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Key == tryL)
                    return entries[i].SourcePart;
        }

        return null;
    }

    /// <summary>稳定池键上的状态系数（乳腺炎/瘀积等）。</summary>
    public static float GetConditionsForPoolKey(this Pawn pawn, string poolKey) =>
        pawn == null || string.IsNullOrEmpty(poolKey)
            ? 1f
            : pawn.GetMilkFlowMultiplierFromConditions(pawn.GetPartForPoolKey(poolKey));

    /// <summary>给定乳房 hediff，返回侧行对应的稳定乳池键；无匹配侧行或无列表下标时 null。</summary>
    public static string GetPoolKeyForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        if (pawn == null || breastHediff == null) return null;
        if (!RjwBreastPoolEconomy.IsBreastHediffForPool(breastHediff)) return null;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].BreastHediff != breastHediff) continue;
            return RjwBreastPoolEconomy.GetVirtualBreastStorageKeyForSideRow(rows[i]);
        }

        BreastPoolTopologyDiagnostics.MaybeDevLogPoolKeyLookupMiss(pawn, breastHediff);
        return null;
    }

    /// <summary>左右池各自的日流速与 RJW 倍率（读 <see cref="CompEquallyMilkable"/> 缓存）。</summary>
    public static (float flowLeft, float flowRight, float multLeft, float multRight) GetFlowPerDayForBreastSides(this Pawn pawn, string leftKey, string rightKey)
    {
        if (pawn == null || pawn.LactatingHediffComp() == null) return (0f, 0f, 0f, 0f);
        var milkComp = pawn.CompEquallyMilkable();
        var entries = pawn.GetResolvedBreastPoolEntries();
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
        var entries = milkComp.GetResolvedBreastPoolEntries();
        if (entries.Count == 0) return 1f;
        var e = entries.FirstOrDefault(x => x.Key == poolKey);
        if (string.IsNullOrEmpty(e.Key)) return 1f;
        float stretch = PoolModelConstants.CapacityStretchCap(e.Capacity);
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

    /// <summary>该乳房 Hediff 涉及的乳池键：默认仅匹配行；虚拟左/右拓扑下额外纳入「同 <see cref="BodyPartRecord.parent"/> 的同胞 Breast/MechBreast 叶」，使悬停 A 只显示 A 对左/右池。</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff) =>
        GetPoolEntriesForBreastHediff(pawn, breastHediff, out _);

    /// <param name="siblingPairHasMultipleLeaves">虚拟左/右且同父下有多片同胞叶时 true，用于悬停追加「本对」范围说明。</param>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff, out bool siblingPairHasMultipleLeaves)
    {
        siblingPairHasMultipleLeaves = false;
        var list = new List<(string, float, float, bool)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || breastHediff == null) return list;
        var rows = RjwBreastPoolEconomy.GetBreastPoolSideRows(pawn);
        var entries = comp.GetResolvedBreastPoolEntries();
        var keys = new HashSet<string>();
        BodyPartRecord clusterParent = null;
        int clusterLateralRows = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (!BreastSideRowMatchesHoveredHediff(rows[i], breastHediff)) continue;
            clusterParent = rows[i].BreastHediff?.Part?.parent;
            break;
        }

        if (clusterParent != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                BodyPartRecord p = r.BreastHediff?.Part;
                if (p == null || p.parent != clusterParent) continue;
                if (!RjwBreastPoolEconomy.IsLateralBreastLeafPart(p)) continue;
                clusterLateralRows++;
                RjwBreastPoolEconomy.AddVirtualBreastStorageKeysForSideRow(r, keys);
            }
        }

        if (keys.Count == 0)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (!BreastSideRowMatchesHoveredHediff(r, breastHediff)) continue;
                RjwBreastPoolEconomy.AddVirtualBreastStorageKeysForSideRow(r, keys);
            }
        }

        siblingPairHasMultipleLeaves = clusterParent != null && clusterLateralRows >= 2;

        foreach (string vk in keys.OrderBy(k => k))
        {
            float cap = RjwBreastPoolEconomy.CapacityForPoolKey(entries, vk);
            float f = comp.GetFullnessForKey(vk);
            bool isLeft = TryIsLeftBreastPoolKey(entries, vk);
            list.Add((vk, f, cap, isLeft));
        }
        return list;
    }

    static bool TryIsLeftBreastPoolKey(List<FluidPoolEntry> entries, string key)
    {
        if (entries == null || string.IsNullOrEmpty(key)) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Key != key) continue;
            return entries[i].IsLeft;
        }

        return false;
    }

    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForPoolKey(this Pawn pawn, string poolKey)
    {
        var list = new List<(string, float, float, bool)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || string.IsNullOrEmpty(poolKey)) return list;
        var entries = comp.GetResolvedBreastPoolEntries();

        void TryAdd(string k)
        {
            if (string.IsNullOrEmpty(k)) return;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Key != k) continue;
                list.Add((k, comp.GetFullnessForKey(k), e.Capacity, e.IsLeft));
                return;
            }
        }

        TryAdd(poolKey);
        if (list.Count > 0) return list;
        if (!RjwBreastPoolEconomy.TryStripVirtualBreastStorageSuffix(poolKey, out _))
        {
            TryAdd(RjwBreastPoolEconomy.AppendVirtualBreastStorageSuffix(poolKey, true));
            TryAdd(RjwBreastPoolEconomy.AppendVirtualBreastStorageSuffix(poolKey, false));
        }
        return list;
    }

    /// <summary>健康表悬停：侧行与当前划过的乳房 Hediff 匹配（引用一致，或 def+部位一致，避免部分 UI 路径引用不同步）。</summary>
    private static bool BreastSideRowMatchesHoveredHediff(RjwBreastPoolSideRow row, Hediff hovered)
    {
        if (hovered == null) return false;
        var bh = row.BreastHediff;
        if (bh == null) return false;
        if (ReferenceEquals(bh, hovered)) return true;
        return bh.def == hovered.def && bh.Part == hovered.Part;
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
