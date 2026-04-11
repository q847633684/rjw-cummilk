using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Cum.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

/// <summary>
/// 穿戴式阴茎采集与 <see cref="Building_Milking"/> 电泵对齐的流速：基准 <see cref="MilkCumSettings.milkingWorkTotalBase"/> 与 <see cref="MilkCumDefOf.EM_MilkingElectric"/> 的采集速度偏移；精池满度用平方压（类比乳侧 f²）。
/// </summary>
public static class MilkingApparelFlowUtility
{
    /// <summary>与 JobDriver 一致：每游戏秒 60 tick，故每 tick 流速 = (池单位/秒) / 60。</summary>
    public const float TicksPerGameSecond = 60f;

    public static float GetElectricMilkingPumpSpeedMultiplier()
    {
        ThingDef def = MilkCumDefOf.EM_MilkingElectric;
        if (def?.equippedStatOffsets == null) return 2f;
        return 1f + def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.AnimalGatherSpeed);
    }

    /// <summary>虚拟精池总满度比值的平方；关闭虚拟池时返回 1（由 Hediff 侧再按日上限封顶）。</summary>
    public static float GetSemenPoolPressureSquared(Pawn pawn)
    {
        if (pawn == null) return 0f;
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return 1f;
        CompVirtualSemenPool comp = pawn.CompVirtualSemenPool();
        var rows = comp.GetSemenPoolDisplayRows(pawn);
        float curSum = 0f;
        float capSum = 0f;
        for (int i = 0; i < rows.Count; i++)
        {
            curSum += rows[i].Current;
            capSum += rows[i].Capacity;
        }

        if (capSum <= 1e-5f) return 0f;
        float f = Mathf.Clamp01(curSum / capSum);
        return f * f;
    }

    /// <summary>与机器挤奶相同的「池单位/秒」基准 × 电泵速度倍率 × 精池压（平方）。</summary>
    public static float GetPenisMilkingFlowPerGameSecond(Pawn pawn)
    {
        float baseFlowPerSecond = 60f / Mathf.Max(0.01f, MilkCumSettings.milkingWorkTotalBase);
        float speedMult = GetElectricMilkingPumpSpeedMultiplier();
        float pressureSq = GetSemenPoolPressureSquared(pawn);
        return baseFlowPerSecond * speedMult * pressureSq;
    }
}
