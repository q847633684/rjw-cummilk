using MilkCum.Core;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.DubsBadHygiene;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>婴儿吮吸 vs 机械挤奶等对泌乳素微刺激的区分。</summary>
public enum MilkingStimulusSource : byte
{
    Generic = 0,
    Baby = 1,
    Machine = 2,
}

/// <summary>拟真子系统 Helpers：SYS-01～09 与 UpdateMilkPools/炎症/RJW 共用。</summary>
public static class MilkRealismHelper
{
    public static float GetComplianceMultiplier(float currentFullness, float stretchCap)
    {
        if (!MilkCumSettings.realismComplianceInflow) return 1f;
        stretchCap = Mathf.Max(1e-4f, stretchCap);
        float r = Mathf.Clamp01(currentFullness / stretchCap);
        float exp = Mathf.Clamp(MilkCumSettings.realismComplianceExponent, 0.35f, 4f);
        float floor = Mathf.Clamp01(MilkCumSettings.realismComplianceFloor);
        return Mathf.Lerp(1f, floor, Mathf.Pow(r, exp));
    }

    /// <summary>单侧进水物理顶格：关闭 SYS-02a 时用 <paramref name="vanillaStretchCap"/>；开启时用基容×(1+额外比例) 且不超过 vanilla。</summary>
    public static float GetPerSideStretchCapFromBase(float baseCapacity, float vanillaStretchCap)
    {
        if (!MilkCumSettings.realismStretchBuffer) return vanillaStretchCap;
        float baseCap = Mathf.Max(0.001f, baseCapacity);
        float extra = Mathf.Clamp(MilkCumSettings.realismStretchExtraFraction, 0f, 0.75f);
        return Mathf.Min(baseCap * (1f + extra), vanillaStretchCap);
    }

    /// <inheritdoc cref="GetPerSideStretchCapFromBase"/>
    public static float GetPerSideStretchCap(FluidPoolEntry e, float vanillaStretchCap) =>
        GetPerSideStretchCapFromBase(e.Capacity, vanillaStretchCap);

    public static float GetCircadianInflowMultiplier(Pawn pawn)
    {
        if (!MilkCumSettings.realismCircadian || pawn?.Map == null) return 1f;
        float hour = GenLocalDate.HourFloat(pawn);
        float phase = MilkCumSettings.realismCircadianPhaseHours;
        float amp = Mathf.Clamp(MilkCumSettings.realismCircadianAmplitude, 0f, 0.45f);
        float w = 2f * Mathf.PI * (hour - phase) / 24f;
        return Mathf.Clamp(1f + amp * Mathf.Sin(w), 0.55f, 1.45f);
    }

    public static float GetMetabolicInflowMultiplier(Pawn pawn)
    {
        if (!MilkCumSettings.realismMetabolicGate || pawn == null) return 1f;
        float mult = 1f;
        if (pawn.needs?.food != null)
        {
            float th = Mathf.Clamp01(MilkCumSettings.realismMetabolicHungerThreshold);
            float lv = pawn.needs.food.CurLevel;
            if (lv < th)
                mult *= Mathf.Clamp01(lv / Mathf.Max(1e-4f, th));
        }
        if (DubsBadHygieneIntegration.TryGetThirstCurLevel01(pawn, out float thirst01))
        {
            float thT = Mathf.Clamp01(MilkCumSettings.realismMetabolicThirstThreshold);
            if (thirst01 < thT)
                mult *= Mathf.Clamp01(thirst01 / Mathf.Max(1e-4f, thT));
        }
        return Mathf.Clamp(mult, 0f, 1f);
    }

    public static float GetDrainReliefInflowBoost(CompEquallyMilkable comp)
    {
        if (!MilkCumSettings.realismEmptyStasisCoupling || comp == null) return 1f;
        int lastG = comp.LastGatheredTick;
        int now = Find.TickManager.TicksGame;
        if (lastG < 0 || now - lastG > Mathf.Max(1, MilkCumSettings.realismDrainReliefWindowTicks))
            return 1f;
        return Mathf.Clamp(MilkCumSettings.realismDrainReliefInflowBoost, 1f, 1.35f);
    }

    /// <summary>乘在炎症 stasis 项上：长期高满略增；近期挤奶/吸奶略降。</summary>
    public static float GetStasisTermScale(CompEquallyMilkable milkComp)
    {
        if (!MilkCumSettings.realismEmptyStasisCoupling || milkComp == null) return 1f;
        float scale = 1f;
        int maxFull = milkComp.GetMaxTicksFullPoolAcrossSides();
        float dayTicks = PoolModelConstants.TicksPerGameDay;
        float longFull = Mathf.Clamp01((maxFull / dayTicks - 0.2f) / 0.8f);
        scale *= 1f + MilkCumSettings.realismStasisLongFullScale * longFull;
        int lastG = milkComp.LastGatheredTick;
        int now = Find.TickManager.TicksGame;
        if (lastG >= 0 && now - lastG <= Mathf.Max(1, MilkCumSettings.realismDrainReliefWindowTicks))
            scale *= Mathf.Clamp(MilkCumSettings.realismStasisReliefScale, 0.4f, 1f);
        return Mathf.Max(0.1f, scale);
    }

    public static float GetStressLetdownMultiplier(Pawn pawn)
    {
        if (!MilkCumSettings.realismStressLetdown || pawn == null) return 1f;
        float mult = 1f;
        if (pawn.health?.hediffSet != null)
        {
            float pain = pawn.health.hediffSet.PainTotal;
            float pStart = Mathf.Max(0.001f, MilkCumSettings.realismStressPainStart);
            float pDrop = Mathf.Clamp01(MilkCumSettings.realismStressPainMaxDrop);
            if (pain > pStart)
                mult *= Mathf.Clamp01(1f - pDrop * Mathf.Clamp01((pain - pStart) / (0.75f - pStart)));
        }
        if (pawn.needs?.mood != null)
        {
            float mood = pawn.needs.mood.CurLevel;
            float th = MilkCumSettings.realismStressMoodThreshold;
            float mDrop = Mathf.Clamp01(MilkCumSettings.realismStressMoodMaxDrop);
            if (mood < th)
                mult *= Mathf.Clamp01(1f - mDrop * Mathf.Clamp01((th - mood) / Mathf.Max(0.05f, th)));
        }
        return Mathf.Clamp(mult, 0.05f, 1f);
    }

    public static float GetMilkingStimulusMultiplier(MilkingStimulusSource source)
    {
        if (!MilkCumSettings.realismStimulusSource) return 1f;
        return source switch
        {
            MilkingStimulusSource.Baby => Mathf.Max(0.05f, MilkCumSettings.realismBabyStimulusMult),
            MilkingStimulusSource.Machine => Mathf.Max(0.05f, MilkCumSettings.realismMachineStimulusMult),
            _ => 1f
        };
    }

    /// <summary>RJW 同步：可选将 t_pool 量化成里程碑，减轻 severity 微抖。</summary>
    public static float QuantizePoolStretchT(float tPool01)
    {
        if (!MilkCumSettings.realismRjwStretchMilestone) return tPool01;
        int steps = Mathf.Max(1, Mathf.RoundToInt(MilkCumSettings.realismRjwStretchMilestoneSteps));
        return Mathf.Floor(Mathf.Clamp01(tPool01) * steps) / steps;
    }

    /// <summary>SYS-10：人类泌乳建立期进水倍率；非人形、永久泌乳基因、未开启设置时为 1。</summary>
    public static float GetLactationEstablishmentInflowMultiplier(Pawn pawn, HediffComp_EqualMilkingLactating lact)
    {
        if (!MilkCumSettings.realismLactationEstablishment || pawn == null || lact?.parent == null) return 1f;
        if (!pawn.RaceProps.Humanlike) return 1f;
        if (MilkCumDefOf.EM_Permanent_Lactation != null && pawn.genes?.HasActiveGene(MilkCumDefOf.EM_Permanent_Lactation) == true)
            return 1f;
        float rampDays = Mathf.Clamp(MilkCumSettings.realismEstablishmentDays, 0.25f, 30f);
        float minMul = Mathf.Clamp(MilkCumSettings.realismEstablishmentMinMult, 0.05f, 1f);
        float ageDays = lact.parent.ageTicks / (float)GenDate.TicksPerDay;
        return Mathf.Lerp(minMul, 1f, Mathf.Clamp01(ageDays / rampDays));
    }
}
