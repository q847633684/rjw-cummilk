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
/// 设计：容量用 Severity×系数；流速倍率用 HediffComp_SexPart.GetFluidMultiplier()（含 partFluidMultiplier，与催乳补品等一致），密度用 sizeProfile.density。不乘 BodySize。由 ExtensionHelper 拆出，见 记忆库/design/架构原则与重组建议。
/// </summary>
public static class PawnMilkPoolExtensions
{
    /// <summary>左池容量：虚拟左池，不修�?Hediff</summary>
    public static float GetLeftBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out float left, out _);
        return left;
    }
    /// <summary>右池容量：虚拟右池，不修�?Hediff</summary>
    public static float GetRightBreastCapacityFactor(this Pawn pawn)
    {
        GetBreastCapacityFactors(pawn, out _, out float right);
        return right;
    }
    /// <summary>左池/右池容量。仅计算层虚拟左右池：无 RJW 乳房 hediff 时不创建乳池，返�?0；否则每�?hediff 表示一对乳房，左右均为所�?hediff 容量之和。不调用 AddHediff/RemoveHediff</summary>
    private static void GetBreastCapacityFactors(Pawn pawn, out float leftFactor, out float rightFactor)
    {
        leftFactor = 0f;
        rightFactor = 0f;
        if (pawn == null) return;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return;
        var list = pawn.GetBreastListOrEmpty();
        if (list.Count == 0) return;
        float coeff = MilkCumSettings.rjwBreastCapacityCoefficient;
        try
        {
            float totalCap = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h?.def == null) continue;
                totalCap += Mathf.Clamp(h.Severity * coeff, 0f, 10f);
            }
            if (totalCap >= 0.01f)
            {
                leftFactor = totalCap;
                rightFactor = totalCap;
            }
        }
        catch { }
    }

    /// <summary>单条乳房 Hediff 的进水倍率：与 RJW 一致（含药品对 partFluidMultiplier 的修正，如 Cumpilation_ActiveMammaries）。</summary>
    private static float GetBreastHediffFlowMultiplier(Hediff h)
    {
        if (h?.def is not HediffDef_SexPart d) return 1f;
        var comp = h.TryGetComp<HediffComp_SexPart>();
        if (comp != null)
            return Mathf.Clamp(comp.GetFluidMultiplier(), 0.1f, 3f);
        return Mathf.Clamp(d.fluidMultiplier, 0.1f, 3f);
    }

    /// <summary>RJW PartSizeConfigDef.density：仅用于「奶量增加」时放大进池量（泌乳进水、高潮产液等），不参与容量/流速倍率。人类 1.0，未配置按 1。</summary>
    public static float GetBreastDensity(HediffDef def)
    {
        if (def is not HediffDef_SexPart sp || sp.sizeProfile?.density == null) return 1f;
        return Mathf.Clamp(sp.sizeProfile.density.Value, 0.5f, 2f);
    }

    /// <summary>按单乳枚举池条目（虚拟左右池，不修改 Hediff）。设计前提：泌乳逻辑仅在「胸部部位有乳房」时进行，即只有存在乳房 hediff（GetBreastList 非空）时才建乳池、进水、挤奶等；无乳房则返回空、不创建默认池。每�?hediff 表示一对，产生 _L/_R 两键，同一对共�?PairIndex。约定：永不返回 null，无乳房时返回空列表。见 记忆�?design/泌乳前提-仅在有乳房时、双池与PairIndex</summary>
    public static List<FluidPoolEntry> GetBreastPoolEntries(this Pawn pawn)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null) return result;
        if (!MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var list = pawn.GetBreastListOrEmpty();
            if (list.Count == 0) return result;
            float coeff = MilkCumSettings.rjwBreastCapacityCoefficient;
            int currentPair = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h?.def == null) continue;
                string partName = h.Part?.def?.defName;
                string baseKey = !string.IsNullOrEmpty(partName) ? partName : h.def.defName;
                string key = baseKey + "_" + i;
                float cap = Mathf.Clamp(h.Severity * coeff, 0f, 10f);
                float mult = GetBreastHediffFlowMultiplier(h);
                float density = GetBreastDensity(h.def);
                result.Add(new FluidPoolEntry(key + "_L", cap, mult, true, currentPair, density));
                result.Add(new FluidPoolEntry(key + "_R", cap, mult, false, currentPair, density));
                currentPair++;
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
            result[i] = new FluidPoolEntry(e.Key, e.Capacity + add, e.FlowMultiplier, e.IsLeft, e.PairIndex, e.Density);
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

    /// <summary>规格：乳腺炎/堵塞等健康影响进水流速。返�?1f 减去乳房不适类 hediff 的惩罚（severity×0.5），最�?0.5。part �?null 时按全身（任一乳腺炎即生效）；part 非空时仅当该部位的乳腺炎生效，对应「哪对乳房的哪一侧」</summary>
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

    /// <summary>该对乳房（poolKey）对应的身体部位，用于按部位的状态（如乳腺炎）判定。无对应 hediff 时返�?null</summary>
    public static BodyPartRecord GetPartForPoolKey(this Pawn pawn, string poolKey)
    {
        if (pawn == null || string.IsNullOrEmpty(poolKey)) return null;
        var list = pawn.GetBreastListOrEmpty();
        if (list.Count == 0) return null;
        foreach (var h in list)
        {
            if (pawn.GetPoolKeyForBreastHediff(h) == poolKey)
                return h.Part;
        }
        return null;
    }

    /// <summary>指定侧（sideKey，如 poolKey_L）的状态系数，用于流速与 UI 按「该乳房该侧」显示</summary>
    public static float GetConditionsForSide(this Pawn pawn, string sideKey)
    {
        if (pawn == null || string.IsNullOrEmpty(sideKey)) return 1f;
        string poolKey = GetPoolKeyFromSideKey(sideKey);
        BodyPartRecord part = pawn.GetPartForPoolKey(poolKey);
        return pawn.GetMilkFlowMultiplierFromConditions(part);
    }

    /// <summary>左乳流速倍率：与容量一致，每个 hediff 表示一对，所�?hediff �?fluidMultiplier 之和计入左。无乳房 hediff �?0</summary>
    public static float GetMilkFlowMultiplierFromRJW_Left(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out float left, out _);
        return left;
    }
    /// <summary>右乳流速倍率：与容量一致，每个 hediff 表示一对，所�?hediff �?fluidMultiplier 之和计入右。无乳房 hediff �?0</summary>
    public static float GetMilkFlowMultiplierFromRJW_Right(this Pawn pawn)
    {
        pawn.GetMilkFlowMultipliersFromRJW(out _, out float right);
        return right;
    }

    /// <summary>左右池流速倍率（与容量一致：每个 hediff 表示一对，�?右均为所�?hediff �?fluidMultiplier 之和）。无 RJW 乳房 hediff �?0/0</summary>
    public static void GetMilkFlowMultipliersFromRJW(this Pawn pawn, out float leftMultiplier, out float rightMultiplier)
    {
        leftMultiplier = 0f;
        rightMultiplier = 0f;
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return;
        try
        {
            var list = pawn.GetBreastListOrEmpty();
            if (list.Count == 0) return;
            float totalMult = 0f;
            foreach (var h in list)
            {
                if (h?.def == null) continue;
                totalMult += GetBreastHediffFlowMultiplier(h);
            }
            if (totalMult > 0f)
            {
                leftMultiplier = totalMult;
                rightMultiplier = totalMult;
            }
        }
        catch { }
    }

    /// <summary>健康页悬停：给定乳房 hediff（须�?GetBreastList 中或与列表中某项 def+Part 一致），返回其对应的池 key（与 GetBreastPoolEntries 一致）。统一�?baseKey_i，每对唯一</summary>
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
        if (i < 0) return null;
        string baseKey = !string.IsNullOrEmpty(breastHediff.Part?.def?.defName) ? breastHediff.Part.def.defName : breastHediff.def.defName;
        return baseKey + "_" + i;
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

    /// <summary>健康页悬停（多乳）：返回�?hediff 对应的所有池条目�? 条为单侧乳，2 条为拆成 _L/_R 的一对），用于显示「左乳：奶量/容量, 右乳：奶�?容量」</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastHediff(this Pawn pawn, Hediff breastHediff)
    {
        var list = new List<(string key, float fullness, float capacity, bool isLeft)>();
        if (pawn?.CompEquallyMilkable() == null || breastHediff == null) return list;
        string key = pawn.GetPoolKeyForBreastHediff(breastHediff);
        if (string.IsNullOrEmpty(key)) return list;
        return pawn.GetPoolEntriesForPoolKey(key);
    }

    /// <summary>健康页悬停（身体部位）：�?poolKey（如 part.def.defName）直接取该对乳房的池条目，不依赖 GetBreastList 中的 hediff。用于悬停「人类乳房」部位时显示�?右乳奶量</summary>
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

    /// <summary>健康页悬停（身体部位）：�?BodyPartRecord 取该部位对应的池条目，用 part.def.defName �?poolKey。无 RJW 乳房 hediff 时人类无对应池，返回空</summary>
    public static List<(string key, float fullness, float capacity, bool isLeft)> GetPoolEntriesForBreastPart(this Pawn pawn, BodyPartRecord part)
    {
        if (pawn?.CompEquallyMilkable() == null || part == null) return new List<(string key, float fullness, float capacity, bool isLeft)>();
        string partDefName = part.def?.defName;
        return string.IsNullOrEmpty(partDefName) ? new List<(string key, float fullness, float capacity, bool isLeft)>() : pawn.GetPoolEntriesForPoolKey(partDefName);
    }

    /// <summary>获取“乳�?胸部”身体部位，用于�?hediff 挂在健康页的乳房行。优�?Breast，否�?Chest（RJW），否则 Torso。无合适部位时返回 null（hediff 将显示为全身）</summary>
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

    /// <summary>获取“生殖器”身体部位，用于将精液相�?hediff 挂在健康页的生殖器行。优先使�?RJW Genital_Helper.get_genitalsBPR，否则从身体中找 Genitals �?Anus。无合适部位时返回 null</summary>
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
