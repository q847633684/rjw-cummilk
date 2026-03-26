using System;
using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Shared.Data;
using MilkCum.RJW;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// RJW 乳房 Hediff 与乳池经济共用：基容量（纯严重度 / 纯 RJW 体积 / 纯 RJW 重量三档）、流速倍率（HediffComp_SexPart × 可选乳头阶段修饰）、池键与按对快照。
/// <see cref="RjwBreastPairSnapshot"/> 为验收要求的「乳房快照」SSOT：容量/流速/UI/RJW Severity 同步均应对齐此结构中的量，避免多文件各写一遍 Severity×系数。
/// </summary>
public static class RjwBreastPoolEconomy
{
    /// <summary>与乳池一致：RJW 乳房定义含 <see cref="GenitalTag.CanLactate"/> 才参与（与 RJW 泌乳标签一致）。</summary>
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

    /// <summary>
    /// 乳池字典的稳定键：自根到 <see cref="Hediff.Part"/> 的 <c>BodyPartDef.defName</c> 路径（<c>/</c> 分隔）。
    /// 可与真实左/右子部位对齐；无 Part 时退化为 <c>hediffDef#index</c>。
    /// </summary>
    public static string MakeStablePoolKey(Hediff h, int listIndex)
    {
        if (h?.def == null) return "";
        BodyPartRecord p = h.Part;
        if (p != null)
        {
            var segments = new List<string>(8);
            for (BodyPartRecord cur = p; cur != null; cur = cur.parent)
                segments.Add(cur.def?.defName ?? "?");
            segments.Reverse();
            return string.Join("/", segments);
        }
        return $"{h.def.defName}#{listIndex}";
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
    /// 单侧虚拟池基容量（组织适应前）：由 <see cref="MilkCumSettings.rjwBreastPoolCapacityMode"/> 在「纯严重度 / 纯 RJW 重量 / 纯 RJW 体积」三选一。
    /// 泌乳流速仍由 <see cref="GetBreastHediffFlowMultiplier"/>（GetFluidMultiplier）单独决定。
    /// 体积/重量档在无法得到有效 RJW 尺寸时结果为 0、不进池。
    /// </summary>
    public static float ComputeBaseCapacityPerSide(Hediff h, float volumeLiters, float weightKg, bool hasBreastSize)
    {
        if (h?.def == null) return 0f;
        float coeff = MilkCumSettings.rjwBreastCapacityCoefficient;
        float sev = GetSeverityForPoolEconomy(h);
        float severityCap = Mathf.Clamp(sev * coeff, 0f, 10f);
        float weightCap = hasBreastSize && weightKg > 0f ? Mathf.Clamp(weightKg * coeff, 0.01f, 10f) : 0f;
        float volumeCap = hasBreastSize && volumeLiters > 0f ? Mathf.Clamp(volumeLiters * coeff, 0.01f, 10f) : 0f;

        return MilkCumSettings.rjwBreastPoolCapacityMode switch
        {
            RjwBreastPoolCapacityMode.RjwBreastWeight => weightCap >= 0.01f ? weightCap : 0f,
            RjwBreastPoolCapacityMode.RjwBreastVolume => volumeCap >= 0.01f ? volumeCap : 0f,
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
                string poolKey = MakeStablePoolKey(h, i);
                bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
                float vol = hasBs ? bs.volume : 0f;
                float w = hasBs ? bs.weight : 0f;
                float cup = hasBs ? bs.cupSize : 0f;
                float band = hasBs ? bs.bandSize : 0f;
                bool usableSize = hasBs && (vol > 0f || w > 0f);
                float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
                if (baseCap <= PoolModelConstants.Epsilon) continue;
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

    internal const string SynthSideLeft = "|L";
    internal const string SynthSideRight = "|R";

    /// <summary>
    /// 左右两条真实池：同一 <see cref="RjwBreastPoolSideRow.PairIndex"/> 的两行由 <see cref="FluidPoolState.TickGrowth"/> 耦合进水。
    /// 键：解剖左/右乳为各自 <see cref="MakeStablePoolKey"/>；单 Hediff 无法区分侧时为 baseKey+|L| / +|R|。
    /// </summary>
    public static List<RjwBreastPoolSideRow> GetBreastPoolSideRows(Pawn pawn)
    {
        var result = new List<RjwBreastPoolSideRow>();
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var list = pawn.GetBreastListOrEmpty();
            var cands = new List<Cand>();
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (!IsBreastHediffForPool(h)) continue;
                bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
                float vol = hasBs ? bs.volume : 0f;
                float w = hasBs ? bs.weight : 0f;
                bool usableSize = hasBs && (vol > 0f || w > 0f);
                float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
                if (baseCap <= PoolModelConstants.Epsilon) continue;
                float flow = GetBreastHediffFlowMultiplier(h);
                cands.Add(new Cand(i, h, h.Part, baseCap, flow));
            }
            if (cands.Count == 0) return result;

            var byParent = new Dictionary<BodyPartRecord, List<Cand>>();
            var noParent = new List<Cand>();
            for (int i = 0; i < cands.Count; i++)
            {
                var c = cands[i];
                BodyPartRecord part = c.Part;
                if (part == null)
                {
                    noParent.Add(c);
                    continue;
                }
                BodyPartRecord pkey = part.parent ?? part;
                if (!byParent.TryGetValue(pkey, out var grp))
                {
                    grp = new List<Cand>();
                    byParent[pkey] = grp;
                }
                grp.Add(c);
            }

            int pairIdx = 0;
            for (int i = 0; i < noParent.Count; i++)
            {
                AppendSyntheticPair(result, noParent[i], pairIdx);
                pairIdx++;
            }

            var parents = new List<BodyPartRecord>(byParent.Keys);
            parents.Sort((a, b) => GroupMinListIndex(byParent[a]).CompareTo(GroupMinListIndex(byParent[b])));

            for (int pi = 0; pi < parents.Count; pi++)
            {
                var grp = byParent[parents[pi]];
                grp.Sort((a, b) => a.ListIndex.CompareTo(b.ListIndex));
                if (grp.Count == 1)
                {
                    AppendSyntheticPair(result, grp[0], pairIdx);
                    pairIdx++;
                    continue;
                }

                Cand leftCand = null, rightCand = null;
                for (int j = 0; j < grp.Count; j++)
                {
                    var cj = grp[j];
                    if (PartNameLooksRight(cj.Part)) { if (rightCand == null) rightCand = cj; }
                    else if (PartNameLooksLeft(cj.Part)) { if (leftCand == null) leftCand = cj; }
                }
                if (leftCand != null && rightCand != null)
                {
                    AppendNaturalSide(result, pairIdx, leftCand, isLeft: true);
                    AppendNaturalSide(result, pairIdx, rightCand, isLeft: false);
                    pairIdx++;
                    AppendOrphansSynthetic(result, grp, leftCand, rightCand, ref pairIdx);
                    continue;
                }

                AppendNaturalSide(result, pairIdx, grp[0], isLeft: true);
                AppendNaturalSide(result, pairIdx, grp[1], isLeft: false);
                pairIdx++;
                var used = new HashSet<int> { grp[0].ListIndex, grp[1].ListIndex };
                for (int k = 2; k < grp.Count; k++)
                {
                    if (used.Contains(grp[k].ListIndex)) continue;
                    AppendSyntheticPair(result, grp[k], pairIdx);
                    pairIdx++;
                    used.Add(grp[k].ListIndex);
                }
            }
        }
        catch
        {
            result.Clear();
        }
        return result;
    }

    private sealed class Cand
    {
        public int ListIndex;
        public Hediff H;
        public BodyPartRecord Part;
        public float BaseCap;
        public float Flow;
        public Cand(int listIndex, Hediff h, BodyPartRecord part, float baseCap, float flow)
        {
            ListIndex = listIndex;
            H = h;
            Part = part;
            BaseCap = baseCap;
            Flow = flow;
        }
    }

    private static int GroupMinListIndex(List<Cand> g)
    {
        int m = int.MaxValue;
        for (int i = 0; i < g.Count; i++)
            if (g[i].ListIndex < m) m = g[i].ListIndex;
        return m;
    }

    private static bool PartNameLooksLeft(BodyPartRecord part)
    {
        if (part?.def?.defName == null) return false;
        return part.def.defName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool PartNameLooksRight(BodyPartRecord part)
    {
        if (part?.def?.defName == null) return false;
        return part.def.defName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void AppendNaturalSide(List<RjwBreastPoolSideRow> result, int pairIdx, Cand c, bool isLeft)
    {
        string key = MakeStablePoolKey(c.H, c.ListIndex);
        if (string.IsNullOrEmpty(key)) return;
        result.Add(new RjwBreastPoolSideRow(pairIdx, key, c.H, c.BaseCap, c.Flow, isLeft));
    }

    private static void AppendSyntheticPair(List<RjwBreastPoolSideRow> result, Cand c, int pairIdx)
    {
        string b = MakeStablePoolKey(c.H, c.ListIndex);
        if (string.IsNullOrEmpty(b)) return;
        result.Add(new RjwBreastPoolSideRow(pairIdx, b + SynthSideLeft, c.H, c.BaseCap, c.Flow, true));
        result.Add(new RjwBreastPoolSideRow(pairIdx, b + SynthSideRight, c.H, c.BaseCap, c.Flow, false));
    }

    private static void AppendOrphansSynthetic(List<RjwBreastPoolSideRow> result, List<Cand> grp, Cand leftCand, Cand rightCand, ref int pairIdx)
    {
        var used = new HashSet<int> { leftCand.ListIndex, rightCand.ListIndex };
        for (int k = 0; k < grp.Count; k++)
        {
            if (used.Contains(grp[k].ListIndex)) continue;
            AppendSyntheticPair(result, grp[k], pairIdx);
            pairIdx++;
        }
    }
}

/// <summary>一条「侧」乳池行：与 <see cref="FluidPoolEntry"/> 一一对应。</summary>
public readonly struct RjwBreastPoolSideRow
{
    public int PairIndex { get; }
    public string PoolKey { get; }
    public Hediff BreastHediff { get; }
    public float BaseCapacity { get; }
    public float FlowMultiplier { get; }
    public bool IsLeft { get; }

    public RjwBreastPoolSideRow(int pairIndex, string poolKey, Hediff breastHediff, float baseCapacity, float flowMultiplier, bool isLeft)
    {
        PairIndex = pairIndex;
        PoolKey = poolKey ?? "";
        BreastHediff = breastHediff;
        BaseCapacity = baseCapacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
    }
}

/// <summary>
/// 一条 RJW 乳房 Hediff 在乳池经济中的只读快照（验收：阶段 1 乳房快照）。
/// ListIndex 为 GetBreastList 下标；PoolKey 为 <see cref="RjwBreastPoolEconomy.MakeStablePoolKey(Verse.Hediff,System.Int32)"/>（部位路径，不含合成 |L|/|R|）。
/// PairIndex 仍为每条泌乳乳房递增的序数；左右真实池与双池耦合见 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/>。
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
