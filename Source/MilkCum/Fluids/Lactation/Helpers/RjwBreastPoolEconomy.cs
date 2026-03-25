using System;
using System.Collections.Generic;
using MilkCum.Core.Settings;
using MilkCum.RJW;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// RJW 乳房 Hediff 与乳池经济共用：基容量（严重度 / RJW 体积或重量 / 混合）、流速倍率（HediffComp_SexPart × 可选乳头阶段修饰）、池键与按对快照。
/// <see cref="RjwBreastPairSnapshot"/> 为验收要求的「乳房快照」SSOT：容量/流速/UI/RJW Severity 同步均应对齐此结构中的量，避免多文件各写一遍 Severity×系数。
/// </summary>
public static class RjwBreastPoolEconomy
{
    /// <summary>与 <see cref="GetBreastPairSnapshots"/> / 池条目一致：RJW 乳房定义含 <see cref="GenitalTag.CanLactate"/> 才参与虚拟池（与 RJW 泌乳标签一致）。</summary>
    public static bool IsBreastHediffForPool(Hediff h)
    {
        if (h?.def is not HediffDef_SexPart sp) return false;
        return sp.genitalTags != null && sp.genitalTags.Contains(GenitalTag.CanLactate);
    }

    /// <summary>容量等经济计算用的「部位严重度」：与 <see cref="HediffComp_SexPart.GetFluidMultiplier"/> 一致优先 <see cref="HediffComp_SexPart.GetSeverity"/>，否则回退 <see cref="Hediff.Severity"/>。</summary>
    public static float GetSeverityForPoolEconomy(Hediff h)
    {
        if (h == null) return 0f;
        if (h.TryGetComp<HediffComp_SexPart>(out var c)) return c.GetSeverity();
        return h.Severity;
    }

    /// <summary>与 <see cref="PawnMilkPoolExtensions.GetBreastPoolEntries"/> 一致：part.defName 优先，否则 hediff.defName，后缀为乳房在 GetBreastList 中的下标。</summary>
    public static string BuildPoolKey(Hediff h, int listIndex)
    {
        if (h?.def == null) return "";
        string partName = h.Part?.def?.defName;
        string baseKey = !string.IsNullOrEmpty(partName) ? partName : h.def.defName;
        return baseKey + "_" + listIndex;
    }

    /// <summary>单条乳房 Hediff 的进水倍率：<see cref="HediffComp_SexPart.GetFluidMultiplier"/> 与 RJW 一致（无额外 0.1–3 夹断），再乘可选乳头阶段修饰；下限仅防 0/负值。</summary>
    public static float GetBreastHediffFlowMultiplier(Hediff h)
    {
        if (h?.def is not HediffDef_SexPart d) return 1f;
        float baseMult;
        if (h.TryGetComp<HediffComp_SexPart>(out var comp))
            baseMult = comp.GetFluidMultiplier();
        else if (h.pawn != null)
        {
            float sev = GetSeverityForPoolEconomy(h);
            baseMult = d.GetFluidMultiplier(sev, 1f, h.pawn.BodySize, SexUtility.ScaleToHumanAge(h.pawn));
        }
        else
            baseMult = d.fluidMultiplier;

        return Mathf.Max(0.01f, baseMult * GetNippleStageFlowDecorMultiplier(h));
    }

    /// <summary>验收阶段 4：RJW 乳房 Hediff 当前阶段标签含 “Nipple” 时，对进水/产液倍率施加小幅修饰（默认关闭，百分比为 0 时不生效）。</summary>
    public static float GetNippleStageFlowDecorMultiplier(Hediff h)
    {
        float p = MilkCumSettings.rjwNippleStageFlowBonusPercent;
        if (Mathf.Abs(p) < 0.001f || h?.CurStage?.label == null) return 1f;
        string lab = h.CurStage.label;
        if (lab.IndexOf("Nipple", StringComparison.OrdinalIgnoreCase) < 0) return 1f;
        return Mathf.Clamp(1f + p / 100f, 0.85f, 1.15f);
    }

    /// <summary>
    /// 单侧虚拟池基容量（组织适应前）：由 <see cref="MilkCumSettings.rjwBreastPoolCapacityMode"/> 在严重度、RJW 重量、RJW 体积与混合之间选择。
    /// 默认安装为 RJW BreastSize.volume（升）×系数；泌乳流速仍由 <see cref="GetBreastHediffFlowMultiplier"/>（GetFluidMultiplier）单独决定。
    /// 重量/体积模式在无法得到正值时回退严重度。
    /// </summary>
    public static float ComputeBaseCapacityPerSide(Hediff h, float volumeLiters, float weightKg, bool hasBreastSize)
    {
        if (h?.def == null) return 0f;
        float coeff = MilkCumSettings.rjwBreastCapacityCoefficient;
        float sev = GetSeverityForPoolEconomy(h);
        float severityCap = Mathf.Clamp(sev * coeff, 0f, 10f);
        float weightCap = hasBreastSize && weightKg > 0f ? Mathf.Clamp(weightKg * coeff, 0.01f, 10f) : 0f;
        float volumeCap = hasBreastSize && volumeLiters > 0f ? Mathf.Clamp(volumeLiters * coeff, 0.01f, 10f) : 0f;
        float alpha = Mathf.Clamp01(MilkCumSettings.rjwBreastCapacityBlendSeverityWeight);

        return MilkCumSettings.rjwBreastPoolCapacityMode switch
        {
            RjwBreastPoolCapacityMode.RjwBreastWeight => weightCap >= 0.01f ? weightCap : severityCap,
            RjwBreastPoolCapacityMode.MaxOfSeverityAndWeight => Mathf.Max(severityCap, weightCap),
            RjwBreastPoolCapacityMode.RjwBreastVolume => volumeCap >= 0.01f ? volumeCap : severityCap,
            RjwBreastPoolCapacityMode.MaxOfSeverityAndVolume => Mathf.Max(severityCap, volumeCap),
            RjwBreastPoolCapacityMode.BlendedSeverityAndVolume =>
                Mathf.Clamp(
                    alpha * severityCap + (1f - alpha) * (volumeCap >= 0.01f ? volumeCap : severityCap),
                    0f, 10f),
            _ => severityCap,
        };
    }

    /// <summary>兼容入口：内部调用 <see cref="PartSizeCalculator.TryGetBreastSize"/> 一次。</summary>
    public static float ComputeBaseCapacityPerSide(Hediff h)
    {
        if (h == null) return 0f;
        bool has = PartSizeCalculator.TryGetBreastSize(h, out var bs);
        float vol = has ? bs.volume : 0f;
        float w = has ? bs.weight : 0f;
        bool usable = has && (vol > 0f || w > 0f);
        return ComputeBaseCapacityPerSide(h, vol, w, usable);
    }

    /// <summary>
    /// 当前小人、在启用 RJW 胸池时的乳房对快照列表；顺序与池条目中 PairIndex 一致。无乳房或关闭设置时返回空表（非 null）。
    /// </summary>
    public static List<RjwBreastPairSnapshot> GetBreastPairSnapshots(Pawn pawn)
    {
        var result = new List<RjwBreastPairSnapshot>();
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var list = pawn.GetBreastListOrEmpty();
            int pair = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (!IsBreastHediffForPool(h)) continue;
                string poolKey = BuildPoolKey(h, i);
                bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
                float vol = hasBs ? bs.volume : 0f;
                float w = hasBs ? bs.weight : 0f;
                float cup = hasBs ? bs.cupSize : 0f;
                float band = hasBs ? bs.bandSize : 0f;
                bool usableSize = hasBs && (vol > 0f || w > 0f);
                float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
                float flow = GetBreastHediffFlowMultiplier(h);
                result.Add(new RjwBreastPairSnapshot(
                    listIndex: i,
                    pairIndex: pair,
                    poolKey: poolKey,
                    breastHediff: h,
                    baseCapacityPerSide: baseCap,
                    flowMultiplier: flow,
                    breastVolumeLiters: vol,
                    breastWeightKg: w,
                    breastCupSize: cup,
                    breastBandSize: band,
                    hasBreastSize: usableSize));
                pair++;
            }
        }
        catch
        {
            result.Clear();
        }
        return result;
    }
}

/// <summary>
/// 一条 RJW 乳房 Hediff 在乳池经济中的只读快照（验收：阶段 1 乳房快照）。
/// ListIndex 为 GetBreastList 下标；PoolKey 无 _L/_R 后缀。
/// </summary>
public readonly struct RjwBreastPairSnapshot
{
    public int ListIndex { get; }
    public int PairIndex { get; }
    public string PoolKey { get; }
    public Hediff BreastHediff { get; }
    public float BaseCapacityPerSide { get; }
    public float FlowMultiplier { get; }
    public float BreastVolumeLiters { get; }
    public float BreastWeightKg { get; }
    public float BreastCupSize { get; }
    public float BreastBandSize { get; }
    public bool HasBreastSize { get; }

    public RjwBreastPairSnapshot(
        int listIndex,
        int pairIndex,
        string poolKey,
        Hediff breastHediff,
        float baseCapacityPerSide,
        float flowMultiplier,
        float breastVolumeLiters,
        float breastWeightKg,
        float breastCupSize,
        float breastBandSize,
        bool hasBreastSize)
    {
        ListIndex = listIndex;
        PairIndex = pairIndex;
        PoolKey = poolKey ?? "";
        BreastHediff = breastHediff;
        BaseCapacityPerSide = baseCapacityPerSide;
        FlowMultiplier = flowMultiplier;
        BreastVolumeLiters = breastVolumeLiters;
        BreastWeightKg = breastWeightKg;
        BreastCupSize = breastCupSize;
        BreastBandSize = breastBandSize;
        HasBreastSize = hasBreastSize;
    }
}
