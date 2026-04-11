using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.RjwBallsOvaries;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// 泌乳池进水纯数值与可复用子步：从 L/设置推导标度、单侧通道与增长-漏奶-回缩子步。
/// 与 <see cref="CompEquallyMilkable.UpdateMilkPools"/> 对齐，便于对照 <c>记忆库/docs/泌乳系统逻辑图</c> 做离屏校验。
/// </summary>
public static class MilkPoolInflowSimulator
{
    /// <summary>单侧进水通道乘子（不含 FlowMultiplier 与 flowPerTickScale）。</summary>
    public readonly struct SideChannelFactors
    {
        public float Conditions { get; }
        public float Pressure { get; }
        public float Letdown { get; }
        public float Duct { get; }

        public SideChannelFactors(float conditions, float pressure, float letdown, float duct)
        {
            Conditions = conditions;
            Pressure = pressure;
            Letdown = letdown;
            Duct = duct;
        }
    }

    /// <summary>若本步应推进进水，返回 true 并给出每日基准流量与每 60tick 初值标度（未乘子步拆分）。</summary>
    /// <param name="drainReliefBoost"><see cref="MilkRealismHelper.GetDrainReliefInflowBoost"/> 等对挤奶后排空进水的加成。</param>
    public static bool TryComputeInflowScale(
        Pawn pawn,
        HediffComp_EqualMilkingLactating lactatingComp,
        float drainReliefBoost,
        out float basePerDayUnscaled,
        out float flowPerTickScale)
    {
        basePerDayUnscaled = 0f;
        flowPerTickScale = 0f;
        if (lactatingComp == null || lactatingComp.RemainingDays <= 0f) return false;
        float currentLactation = lactatingComp.CurrentLactationAmount;
        float effectiveLForFlow = lactatingComp.EffectiveLactationAmountForFlow;
        if (currentLactation <= 0f || pawn == null) return false;
        float hungerFactor = Mathf.Max(0.05f, PawnUtility.BodyResourceGrowthSpeed(pawn));
        float drive = MilkCumSettings.GetEffectiveDrive(effectiveLForFlow);
        float raceFlow = MilkCumSettings.defaultFlowMultiplierForHumanlike;
        basePerDayUnscaled = drive * hungerFactor * raceFlow;
        basePerDayUnscaled *= MilkRealismHelper.GetLactationEstablishmentInflowMultiplier(pawn, lactatingComp);
        flowPerTickScale = ApplyPawnWideInflowMultipliers(
            pawn,
            drainReliefBoost,
            Mathf.Max(0f, basePerDayUnscaled / PoolModelConstants.TicksPerGameDay * PoolModelConstants.Interval60Ticks));
        return true;
    }

    public static float ApplyPawnWideInflowMultipliers(Pawn pawn, float drainReliefBoost, float flowPerTickScaleFromBase)
    {
        float s = flowPerTickScaleFromBase;
        s *= MilkRealismHelper.GetCircadianInflowMultiplier(pawn);
        s *= MilkRealismHelper.GetMetabolicInflowMultiplier(pawn);
        s *= RjwBallsOvariesIntegration.GetLactationInflowMultiplier(pawn);
        s *= drainReliefBoost;
        return s;
    }

    /// <summary>炎症 I 相对 I_crit 归一化到 [0,1]，供进水/挤出导管共用。</summary>
    public static float GetNormalizedInflammation01(HediffComp_EqualMilkingLactating lact, string poolKey)
    {
        if (lact == null || string.IsNullOrEmpty(poolKey)) return 0f;
        return Mathf.Clamp01(lact.GetInflammationForKey(poolKey) / Mathf.Max(0.01f, MilkCumSettings.inflammationCrit));
    }

    public static SideChannelFactors ComputeSideChannelFactors(
        Pawn pawn,
        FluidPoolEntry entry,
        float currentFullness,
        float stretchCap,
        HediffComp_EqualMilkingLactating lactatingComp,
        float residualL,
        FluidPoolNetwork network)
    {
        float pressure = MilkCumSettings.enablePressureFactor
            ? MilkCumSettings.GetPressureFactor(currentFullness / Mathf.Max(0.001f, stretchCap))
            : (currentFullness >= stretchCap ? 0f : 1f);
        MilkCumSettings.ApplyOverflowResidualFlow(ref pressure, currentFullness, stretchCap, residualL, lactatingComp.GetInflammationForKey(entry.Key));
        float conditions = pawn.GetConditionsForPoolKey(entry.Key);
        float letdown = MilkCumSettings.enableLetdownReflex ? lactatingComp.GetLetdownReflexFlowMultiplier(entry.Key) : 1f;
        letdown *= MilkRealismHelper.GetStressLetdownMultiplier(pawn);
        float duct = ComputeDuctConductance(pawn, entry, network, lactatingComp);
        return new SideChannelFactors(conditions, pressure, letdown, duct);
    }

    public static float BreastSideCoreRate(FluidPoolEntry entry, in SideChannelFactors chan) =>
        entry.FlowMultiplier * chan.Conditions * chan.Pressure * chan.Letdown * chan.Duct;

    public static (float flowPerTick, float pressure, float conditions, float letdown) ComputeSideFlow(
        Pawn pawn,
        FluidPoolEntry entry,
        float currentFullness,
        float stretchCap,
        float flowPerTickScale,
        HediffComp_EqualMilkingLactating lactatingComp,
        float residualL,
        FluidPoolNetwork network)
    {
        var chan = ComputeSideChannelFactors(pawn, entry, currentFullness, stretchCap, lactatingComp, residualL, network);
        float compliance = MilkRealismHelper.GetComplianceMultiplier(currentFullness, stretchCap);
        float core = BreastSideCoreRate(entry, chan);
        float flowPerTick = Mathf.Max(0f, core * flowPerTickScale) * compliance;
        return (flowPerTick, chan.Pressure, chan.Conditions, chan.Letdown);
    }

    public static float ComputeDuctConductance(
        Pawn pawn,
        FluidPoolEntry entry,
        FluidPoolNetwork network,
        HediffComp_EqualMilkingLactating lactatingComp = null)
    {
        if (string.IsNullOrEmpty(entry.Key)) return 1f;
        float outlet = network?.GetOutletHopFactor(entry.Key, MilkCumSettings.ductHopPenaltyPerEdge) ?? 1f;
        lactatingComp ??= pawn?.LactatingHediffComp();
        float inflammation = GetNormalizedInflammation01(lactatingComp, entry.Key);
        float baseResistance = 1f + inflammation * MilkCumSettings.ductInflowInflammationResistance;
        float conductance = outlet / Mathf.Max(MilkCumSettings.ductConductanceMin, baseResistance);
        return Mathf.Clamp(conductance, MilkCumSettings.ductConductanceMin, MilkCumSettings.ductConductanceMax);
    }

    public static float ApplyGrowthLeakShrinkSubstep(
        FluidPoolEntry e,
        string poolKey,
        ref float current,
        float flowPerTick,
        float stretchCap,
        float pressureForLeak,
        HediffComp_EqualMilkingLactating lactatingComp,
        float shrinkFactor,
        ref float reabsorbedPoolThisStep,
        Dictionary<string, float> reabsorbedPerKeyCache)
    {
        float sideOverflowAcc = 0f;
        var growth = FluidPoolState.SingleBreastTickGrowth(current, flowPerTick, e.Capacity, stretchCap);
        current = growth.newFullness;
        sideOverflowAcc += growth.overflow;
        if (MilkCumSettings.realismReflexLeak && flowPerTick > PoolModelConstants.Epsilon)
        {
            float pk = Mathf.Clamp01(current / Mathf.Max(1e-4f, stretchCap));
            float ik = lactatingComp.GetInflammationForKey(poolKey);
            float ic = Mathf.Max(0.01f, MilkCumSettings.inflammationCrit);
            float infMul = 1f + MilkCumSettings.realismLeakInflammationScale * Mathf.Clamp01(ik / ic);
            float expect = flowPerTick * (MilkCumSettings.realismLeakBaseRate + MilkCumSettings.realismLeakPressureScale * pressureForLeak)
                * (1f + MilkCumSettings.realismLeakFullnessScale * pk) * infMul;
            float varL = Mathf.Clamp01(MilkCumSettings.realismLeakVariance);
            expect *= Rand.Range(1f - varL, 1f + varL);
            float leak = Mathf.Min(current, Mathf.Max(0f, expect));
            current -= leak;
            sideOverflowAcc += leak;
        }

        if (current > e.Capacity)
        {
            float excess = current - e.Capacity;
            float reabs = excess * (1f - shrinkFactor);
            reabsorbedPoolThisStep += reabs;
            reabsorbedPerKeyCache[poolKey] = (reabsorbedPerKeyCache.TryGetValue(poolKey, out var old) ? old : 0f) + reabs;
            current = e.Capacity + excess * shrinkFactor;
        }

        return sideOverflowAcc;
    }
}
