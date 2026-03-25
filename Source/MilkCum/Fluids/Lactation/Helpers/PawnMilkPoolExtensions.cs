using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.RJW;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 乳池与流速：池条目、容量、MilkDef、泌乳 Hediff 访问、流速倍率（RJW/基因/乳腺炎）、压奶满度、身体部位、距离等。
/// 设计：乳房对与池键以 <see cref="RjwBreastPairSnapshot"/> / <see cref="RjwBreastPoolEconomy.GetBreastPairSnapshots"/> 为 SSOT。不乘 BodySize。由 ExtensionHelper 拆出，见 记忆库/design/架构原则与重组建议。
/// </summary>
public static class PawnMilkPoolExtensions
{
    /// <summary>左池容量：虚拟左池，不修改 Hediff</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右池容量：虚拟右池，不修改 Hediff</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>左池/右池容量（汇总用）。无 RJW 乳房 hediff 时返回 0；否则为所有乳房对的单侧基容量之和，左右各写同值（多对乳时非解剖「仅左胸」语义，见记忆库文档「汇总 API 注意」）。不修改 Hediff</summary>
    private static void GetBreastCapacityFactors(Pawn pawn, out float leftFactor, out float rightFactor)
    {
        leftFactor = 0f;
        rightFactor = 0f;
        if (pawn == null) return;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return;
        try
        {
            var snaps = RjwBreastPoolEconomy.GetBreastPairSnapshots(pawn);
            if (snaps.Count == 0) return;
            float totalCap = 0f;
            for (int i = 0; i < snaps.Count; i++)
                totalCap += snaps[i].BaseCapacityPerSide;
            if (totalCap >= 0.01f)
            {
                leftFactor = totalCap;
                rightFactor = totalCap;
            }
        }
        catch { }
    }

    /// <summary>枚举虚拟左/右乳池条目（不修改 Hediff）。前提：GetBreastList 非空才建池。每条乳房 Hediff 生成 poolKey_L / poolKey_R，共用 PairIndex。永不返回 null。见记忆库 design/泌乳前提、双池与 PairIndex</summary>
    public static List<FluidPoolEntry> GetBreastPoolEntries(this Pawn pawn)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null) return result;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var snaps = RjwBreastPoolEconomy.GetBreastPairSnapshots(pawn);
            if (snaps.Count == 0) return result;
            for (int i = 0; i < snaps.Count; i++)
            {
                var s = snaps[i];
                string key = s.PoolKey;
                // 每条乳房 Hediff 固定拆成虚拟左/右池：容量、流速、炎症、涨奶、喷乳、扣奶等均按侧 key（_L/_R）独立；泌乳 L/剩余天数等仍在 Lactating Hediff 上全局。
                result.Add(new FluidPoolEntry(key + "_L", s.BaseCapacityPerSide, s.FlowMultiplier, true, s.PairIndex));
                result.Add(new FluidPoolEntry(key + "_R", s.BaseCapacityPerSide, s.FlowMultiplier, false, s.PairIndex));
            }
            ApplyCapacityAdaptationToEntries(pawn, result);
        }
        catch
        {
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
            result[i] = new FluidPoolEntry(e.Key, e.Capacity + add, e.FlowMultiplier, e.IsLeft, e.PairIndex);
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

    /// <summary>乳腺炎/瘀积等对流速的惩罚：返回 1 减去有关 Def 的 severity×权重，有下限。part 为 null 时查全身任一条；part 非空时仅匹配该部位（虚拟 _L/_R 共用该对 Part，故共敏同一乳腺炎 Hediff）</summary>
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

    /// <summary>从侧 key（如 HumanBreast_L）得到池 key（HumanBreast）</summary>
    public static string GetPoolKeyFromSideKey(string sideKey)
    {
        if (string.IsNullOrEmpty(sideKey)) return sideKey;
        if (sideKey.EndsWith("_L") || sideKey.EndsWith("_R"))
            return sideKey.Substring(0, sideKey.Length - 2);
        return sideKey;
    }

    /// <summary>该对乳房（poolKey）对应的身体部位，用于按部位的状态（如乳腺炎）判定。无对应 hediff 时返回 null</summary>
    public static BodyPartRecord GetPartForPoolKey(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return null;
        var snaps = RjwBreastPoolEconomy.GetBreastPairSnapshots(pawn);
        for (int i = 0; i < snaps.Count; i++)
        {
            if (snaps[i].PoolKey == poolKey)
                return snaps[i].BreastHediff?.Part;
        }
        return null;
    }

    /// <summary>指定侧（sideKey，如 poolKey_L）的状态系数，用于流速与 UI 按「该乳房该侧」显示</summary>
    public static float GetConditionsForSide(this Pawn pawn, string sideKey)
    {
        if (pawn == null || string.IsNullOrEmpty(sideKey)) return 1f;
        if (!sideKey.EndsWith("_L") && !sideKey.EndsWith("_R"))
        {
            BodyPartRecord partDirect = pawn.GetPartForPoolKey(sideKey);
            return pawn.GetMilkFlowMultiplierFromConditions(partDirect);
        }
        string poolKey = GetPoolKeyFromSideKey(sideKey);
        BodyPartRecord part = pawn.GetPartForPoolKey(poolKey);
        return pawn.GetMilkFlowMultiplierFromConditions(part);
    }

    /// <summary>健康页悬停：给定乳房 hediff（须属于 GetBreastList 或与列表项 def+Part 一致），返回 poolKey（与 GetBreastPoolEntries / BuildPoolKey 一致）</summary>
    public static string GetPoolKeyForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        if (pawn == null || breastHediff == null) return null;
        var list = pawn.GetBreastListOrEmpty();
        if (list.Count == 0) return null;
        int i = list.IndexOf(breastHediff);
        if (i < 0)
        {
            for (int j = 0; j < list.Count; j++)
            {
                var h = list[j];
                if (h?.def == breastHediff.def && h.Part == breastHediff.Part)
                {
                    i = j;
                    break;
                }
            }
        }
        if (i < 0 || !RjwBreastPoolEconomy.IsBreastHediffForPool(breastHediff)) return null;
        string key = RjwBreastPoolEconomy.BuildPoolKey(breastHediff, i);
        return string.IsNullOrEmpty(key) ? null : key;
    }

    /// <summary>该对乳房的左右乳产奶流速（池单位/天）及各自流速倍率；仅读池逻辑每侧缓存，缓存未刷新时返回 0 流速（逻辑由 UpdateMilkPools 统一维护）。entries 优先复用 Comp 的缓存，避免重复构建列表。</summary>
    public static (float flowLeft, float flowRight, float multLeft, float multRight) GetFlowPerDayForBreastPair(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return (0f, 0f, 0f, 0f);
        if (pawn.LactatingHediffComp() == null) return (0f, 0f, 0f, 0f);
        var milkComp = pawn.CompEquallyMilkable();
        var entries = milkComp?.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        if (entries.Count == 0) return (0f, 0f, 0f, 0f);
        string keyL = poolKey + "_L", keyR = poolKey + "_R";
        float multL = 0f, multR = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Key == keyL) multL = e.FlowMultiplier;
            else if (e.Key == keyR) multR = e.FlowMultiplier;
        }
        if (milkComp == null || !milkComp.IsCachedFlowValid())
            return (0f, 0f, multL, multR);
        return (milkComp.GetFlowPerDayForKeyCached(keyL), milkComp.GetFlowPerDayForKeyCached(keyR), multL, multR);
    }

    /// <summary>指定侧（poolKey_L / poolKey_R）的压力系数，用于健康页因子行按侧显示。P = 该侧满度/该侧撑大容量</summary>
    public static float GetPressureFactorForSide(this Pawn pawn, string sideKey)
    {
        var milkComp = pawn?.CompEquallyMilkable();
        if (milkComp == null || string.IsNullOrEmpty(sideKey)) return 1f;
        var entries = milkComp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        if (entries.Count == 0) return 1f;
        var e = entries.FirstOrDefault(x => x.Key == sideKey);
        if (string.IsNullOrEmpty(e.Key)) return 1f;
        float stretch = e.Capacity * PoolModelConstants.StretchCapFactor;
        float fullE = milkComp.GetFullnessForKey(sideKey);
        float pressure = MilkCumSettings.enablePressureFactor
            ? MilkCumSettings.GetPressureFactor(fullE / Mathf.Max(0.001f, stretch))
            : (fullE >= stretch ? 0f : 1f);
        var lact = pawn.LactatingHediffComp();
        float resL = lact?.CurrentLactationAmount ?? 0f;
        float resI = lact?.GetInflammationForKey(sideKey) ?? 0f;
        MilkCumSettings.ApplyOverflowResidualFlow(ref pressure, fullE, stretch, resL, resI);
        return pressure;
    }

    /// <summary>健康页悬停：该乳房 hediff 对应池条目，正常为 2 条（poolKey_L、poolKey_R）</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        var list = new List<(string key, float fullness, float capacity, bool isLeft)>();
        if (pawn?.CompEquallyMilkable() == null || breastHediff == null) return list;
        string key = pawn.GetPoolKeyForBreastHediff(breastHediff);
        if (string.IsNullOrEmpty(key)) return list;
        return pawn.GetPoolEntriesForPoolKey(key);
    }

    /// <summary>健康页悬停（身体部位）：按 poolKey（如 part.def.defName）取该对乳房池条目。用于悬停部位时显示左/右乳奶量</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForPoolKey(this Pawn pawn, string poolKey)
    {
        var list = new List<(string key, float fullness, float capacity, bool isLeft)>();
        var comp = pawn?.CompEquallyMilkable();
        if (comp == null || string.IsNullOrEmpty(poolKey)) return list;
        var entries = comp.GetCachedEntriesIfValid() ?? pawn.GetBreastPoolEntries();
        foreach (var e in entries)
        {
            if (e.Key != poolKey && e.Key != poolKey + "_L" && e.Key != poolKey + "_R") continue;
            float f = comp.GetFullnessForKey(e.Key);
            list.Add((e.Key, f, e.Capacity, e.IsLeft));
        }
        return list;
    }

    /// <summary>健康页悬停（身体部位）：由 BodyPartRecord 取池条目，以 part.def.defName 作 poolKey。无对应 RJW 乳房 hediff 时返回空</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastPart(this Pawn pawn, BodyPartRecord part)
    {
        if (pawn?.CompEquallyMilkable() == null || part == null) return new List<(string key, float fullness, float capacity, bool isLeft)>();
        string partDefName = part.def?.defName;
        return string.IsNullOrEmpty(partDefName) ? new List<(string key, float fullness, float capacity, bool isLeft)>() : pawn.GetPoolEntriesForPoolKey(partDefName);
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
        catch { }
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
