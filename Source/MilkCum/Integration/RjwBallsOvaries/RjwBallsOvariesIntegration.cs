using System;
using MilkCum.Core.Settings;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Integration.RjwBallsOvaries;

/// <summary>
/// 与 TeheeItsMe525.RJWGenderOrgansMod 联动，按可辩护的生理方向简化：有机睾丸体量略影响储精上限；睾酮仅在有活体睾丸组织时调节生精/回充；去势与去势环按缺血坏死逻辑；装备只体现压迫与机械梗阻，不奖励「情趣增产量」；雌激素对泌乳仅作极小的背景修饰（泌乳主因仍为本 mod 催乳素/泌乳轴）。
/// 未加载该模组或总开关关闭时所有乘子为中性值。
/// </summary>
[StaticConstructorOnStartup]
public static class RjwBallsOvariesIntegration
{
    private static readonly BodyPartDef GonadsPartDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Gonads");

    static RjwBallsOvariesIntegration()
    {
        IsModActive = ModLister.GetModWithIdentifier("TeheeItsMe525.RJWGenderOrgansMod") != null
            || DefDatabase<HediffDef>.GetNamedSilentFail("GenericTesticles") != null;
    }

    public static bool IsModActive { get; }

    public static bool Applies(Pawn pawn) =>
        IsModActive && MilkCumSettings.Integration_Ballz_Enable && pawn?.health?.hediffSet != null;

    /// <summary>虚拟精池左右槽总容量的额外乘子（在 RJW 阴茎体量汇总之后）。有机睾丸仅作小幅修正；假体不分泌，容量显著压低。</summary>
    public static float GetVirtualSemenCapacityMultiplier(Pawn pawn)
    {
        if (!Applies(pawn) || !MilkCumSettings.Integration_Ballz_GonadSemenCapacity)
            return 1f;
        float organic = MaxOrganicTesticleSeverity01(pawn);
        float m = 1f;
        if (organic > 0.001f)
            m = Mathf.Lerp(0.96f, 1.03f, Mathf.Clamp01(organic));
        else
        {
            float prost = ProstheticTesticleCapacityFactor(pawn);
            if (prost >= 0f) m = prost;
        }

        if (MilkCumSettings.Integration_Ballz_TesticleGear)
            m *= TesticleGearCapacityMultiplier(pawn);
        return Mathf.Clamp(m, 0.08f, 1.05f);
    }

    /// <summary>精池每 tick 回充在基础公式上的乘子（睾酮、去势环、恢复期等）。</summary>
    public static float GetVirtualSemenRefillMultiplier(Pawn pawn)
    {
        if (!Applies(pawn)) return 1f;
        if (MilkCumSettings.Integration_Ballz_NeuteredNoSemen && HasNeutered(pawn))
            return 0f;
        float m = 1f;
        if (MilkCumSettings.Integration_Ballz_TestosteroneRefill)
            m *= TestosteroneRefillFactor(pawn);
        if (MilkCumSettings.Integration_Ballz_ElastrationPenalty)
            m *= ElastrationRefillFactor(pawn);
        m *= RecoveringFromElastrationFactor(pawn);
        if (MilkCumSettings.Integration_Ballz_TesticleGear)
            m *= TesticleGearRefillMultiplier(pawn);
        return Mathf.Clamp(m, 0f, 2f);
    }

    /// <summary>从虚拟池可取出的比例；去势后为 0（与回充为 0 配合，池会逐渐用尽）。</summary>
    public static float GetVirtualSemenDrawFactor(Pawn pawn)
    {
        if (!Applies(pawn)) return 1f;
        if (MilkCumSettings.Integration_Ballz_NeuteredNoSemen && HasNeutered(pawn))
            return 0f;
        if (MilkCumSettings.Integration_Ballz_TesticleGear)
            return Mathf.Clamp(TesticleGearDrawMultiplier(pawn), 0f, 1f);
        return 1f;
    }

    /// <summary>泌乳全局进水乘子（在昼夜节律、代谢等之后）。</summary>
    public static float GetLactationInflowMultiplier(Pawn pawn)
    {
        if (!Applies(pawn) || !MilkCumSettings.Integration_Ballz_EstrogenLactation)
            return 1f;
        float fromEstrogen = EstrogenLactationFactor(pawn);
        return Mathf.Clamp(fromEstrogen, 0.98f, 1.02f);
    }

    public static bool ShowSemenTooltipHint(Pawn pawn) =>
        Applies(pawn)
        && MilkCumSettings.Cum_EnableVirtualSemenPool
        && (Mathf.Abs(GetVirtualSemenRefillMultiplier(pawn) - 1f) > 0.025f
            || Mathf.Abs(GetVirtualSemenCapacityMultiplier(pawn) - 1f) > 0.025f
            || GetVirtualSemenDrawFactor(pawn) < 0.99f);

    public static string SemenTooltipHintFor(Pawn pawn)
    {
        if (!ShowSemenTooltipHint(pawn)) return null;
        float cap = GetVirtualSemenCapacityMultiplier(pawn);
        float refill = GetVirtualSemenRefillMultiplier(pawn);
        float draw = GetVirtualSemenDrawFactor(pawn);
        return "EM.BallzSemenPoolModifiersTip".Translate(
            cap.ToStringPercent(),
            refill.ToStringPercent(),
            draw.ToStringPercent());
    }

    private static bool HasNeutered(Pawn pawn)
    {
        HediffDef d = DefDatabase<HediffDef>.GetNamedSilentFail("Neutered");
        return d != null && pawn.health.hediffSet.HasHediff(d);
    }

    private static float MaxOrganicTesticleSeverity01(Pawn pawn)
    {
        float max = 0f;
        foreach (Hediff h in pawn.health.hediffSet.hediffs)
        {
            if (h?.def is not HediffDef_SexPart) continue;
            if (!IsGonadsPart(h.Part)) continue;
            string dn = h.def.defName;
            if (dn == null || dn.IndexOf("Testicle", StringComparison.OrdinalIgnoreCase) < 0) continue;
            max = Mathf.Max(max, h.Severity);
        }

        return Mathf.Clamp01(max);
    }

    private static float ProstheticTesticleCapacityFactor(Pawn pawn)
    {
        float best = -1f;
        foreach (Hediff h in pawn.health.hediffSet.hediffs)
        {
            if (h?.def == null || !IsGonadsPart(h.Part)) continue;
            string dn = h.def.defName;
            if (dn == "WoodTesticles") best = Mathf.Max(best, 0.12f);
            else if (dn == "SteelTesticles") best = Mathf.Max(best, 0.18f);
            else if (dn == "ArchotechTesticles") best = Mathf.Max(best, 0.28f);
        }

        return best;
    }

    private static bool IsGonadsPart(BodyPartRecord part) =>
        part != null && GonadsPartDef != null && part.def == GonadsPartDef;

    private static float TestosteroneRefillFactor(Pawn pawn)
    {
        HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("TestosteroneEffect");
        if (def == null) return 1f;
        Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (h == null) return 1f;
        if (MilkCumSettings.Integration_Ballz_TestosteroneRequiresPenisPool
            && MaxOrganicTesticleSeverity01(pawn) <= 0.001f)
            return 1f;
        float s = Mathf.Clamp01(h.Severity);
        return Mathf.Lerp(0.90f, 1.04f, s);
    }

    /// <summary>装备不改变「储精上限」的生理基础，仅对紧缚类给极小的象征性压缩（无增容奖励）。</summary>
    private static float TesticleGearCapacityMultiplier(Pawn pawn)
    {
        float m = 1f;
        if (HasGear(pawn, "WearingBallCage")) m *= 0.96f;
        if (HasGear(pawn, "WearingTightBinding")) m *= 0.95f;
        return Mathf.Clamp(m, 0.88f, 1f);
    }

    private static float TesticleGearRefillMultiplier(Pawn pawn)
    {
        float m = 1f;
        if (HasGear(pawn, "WearingBallStretcher")) m *= 0.86f;
        if (HasGear(pawn, "WearingWeightedRings")) m *= 0.84f;
        if (HasGear(pawn, "WearingComfortHarness")) m *= 1f;
        if (HasGear(pawn, "WearingBallCage")) m *= 0.40f;
        if (HasGear(pawn, "WearingTightBinding")) m *= 0.46f;
        if (HasGear(pawn, "WearingBallSeparator")) m *= 0.78f;
        return Mathf.Clamp(m, 0.18f, 1f);
    }

    private static float TesticleGearDrawMultiplier(Pawn pawn)
    {
        float m = 1f;
        if (HasGear(pawn, "WearingBallCage")) m *= 0.68f;
        if (HasGear(pawn, "WearingTightBinding")) m *= 0.76f;
        if (HasGear(pawn, "WearingBallSeparator")) m *= 0.86f;
        if (HasGear(pawn, "WearingBallStretcher")) m *= 0.93f;
        if (HasGear(pawn, "WearingWeightedRings")) m *= 0.92f;
        return Mathf.Clamp(m, 0.38f, 1f);
    }

    private static bool HasGear(Pawn pawn, string defName)
    {
        HediffDef d = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
        return d != null && pawn.health.hediffSet.HasHediff(d);
    }

    private static float ElastrationRefillFactor(Pawn pawn)
    {
        HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("Elastrated");
        if (def == null) return 1f;
        Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (h == null) return 1f;
        float s = Mathf.Clamp01(h.Severity);
        if (s < 0.22f) return Mathf.Lerp(0.88f, 0.68f, s / 0.22f);
        return Mathf.Lerp(0.68f, 0.05f, Mathf.InverseLerp(0.22f, 0.92f, s));
    }

    private static float RecoveringFromElastrationFactor(Pawn pawn)
    {
        HediffDef d = DefDatabase<HediffDef>.GetNamedSilentFail("RecoveringFromElastration");
        if (d == null || !pawn.health.hediffSet.HasHediff(d)) return 1f;
        return 0.84f;
    }

    /// <summary>雌激素与乳腺发育相关，但不驱动泌乳主流量；仅作极小背景修饰（泌乳仍主要由催乳素/泌乳 Hediff 与设置决定）。</summary>
    private static float EstrogenLactationFactor(Pawn pawn)
    {
        HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("EstrogenEffect");
        if (def == null) return 1f;
        Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (h == null) return 1f;
        float s = Mathf.Clamp01(h.Severity);
        return Mathf.Lerp(0.993f, 1.007f, s);
    }
}
