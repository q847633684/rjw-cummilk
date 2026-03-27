using System;
using System.Collections.Generic;
using System.Text;
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
/// RJW 乳房 Hediff 与乳池经济共用：基容量（纯严重度 / 纯 RJW 体积 / 纯 RJW 重量三档）、流速倍率（HediffComp_SexPart × 可选乳头阶段修饰）、池键与乳房快照。
/// <see cref="RjwBreastPoolSnapshot"/> 为验收要求的「乳房快照」SSOT：容量/流速/UI/RJW Severity 同步均应对齐此结构中的量，避免多文件各写一遍 Severity×系数。
/// </summary>
public static class RjwBreastPoolEconomy
{
    /// <summary>开发模式下记录乳池构建异常，避免静默清空列表。</summary>
    public static void LogDev(string context, Exception ex)
    {
        if (ex == null || !Prefs.DevMode) return;
        Log.Warning($"[MilkCum] {context}: {ex.Message}");
    }

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
    /// 叶部位含 <see cref="BodyPartRecord.customLabel"/> 时拼入，避免左右同 Def（如双 Breast）键冲突。
    /// 无 Part 时退化为 <c>hediffDef#index</c>。
    /// </summary>
    public static string MakeStablePoolKey(Hediff h, int listIndex)
    {
        if (h?.def == null) return "";
        BodyPartRecord p = h.Part;
        if (p != null)
        {
            var segments = new List<string>(8);
            for (BodyPartRecord cur = p; cur != null; cur = cur.parent)
            {
                string seg = cur.def?.defName ?? "?";
                if (cur == p && !string.IsNullOrEmpty(cur.customLabel))
                {
                    seg = seg + "_" + cur.customLabel.Replace(" ", "_").Replace("/", "_");
                }

                segments.Add(seg);
            }

            segments.Reverse();
            return string.Join("/", segments);
        }
        return $"{h.def.defName}#{listIndex}";
    }

    /// <summary>当前泌乳乳房列表的稳定键签名（条数不变时 Part/路径变化也会变），供条目缓存失效检测。</summary>
    public static string BuildBreastListPoolKeySignature(Pawn pawn)
    {
        if (pawn == null) return "";
        var list = pawn.GetBreastListOrEmpty();
        if (list.Count == 0) return "";
        var sb = new StringBuilder(list.Count * 48);
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append(MakeStablePoolKey(list[i], i));
        }

        return sb.ToString();
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
    /// 当前小人、在启用 RJW 胸池时的乳房快照；与 <see cref="GetBreastPoolSideRows"/> 一致。
    /// 当前模型每条乳房子部位独立一条快照，<see cref="RjwBreastPoolSnapshot.PoolIndex"/> 按遍历顺序递增。
    /// </summary>
    public static List<RjwBreastPoolSnapshot> GetBreastPoolSnapshots(Pawn pawn)
    {
        var result = new List<RjwBreastPoolSnapshot>();
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var cands = BuildBreastPoolCandidates(pawn);
            if (cands.Count == 0) return result;
            cands.Sort((a, b) => a.ListIndex.CompareTo(b.ListIndex));
            int poolIdx = 0;
            for (int i = 0; i < cands.Count; i++)
            {
                // 新模型：一条乳房子部位 = 一个池（不再按父节点做左右配对）。
                AppendBreastPoolSnapshot(result, cands[i], poolIdx);
                poolIdx++;
            }
        }
        catch (Exception ex)
        {
            LogDev(nameof(GetBreastPoolSnapshots), ex);
            result.Clear();
        }
        return result;
    }

    /// <summary>
    /// 每条可泌乳叶一条真实池行；当前模型每条乳房子部位独立一池并独立计算。
    /// Breast/MechBreast 叶；无部位、旧 Chest 单点、容量为 0 等仍不进池。
    /// </summary>
    public static List<RjwBreastPoolSideRow> GetBreastPoolSideRows(Pawn pawn)
    {
        var result = new List<RjwBreastPoolSideRow>();
        if (pawn == null || !MilkCumSettings.rjwBreastSizeEnabled) return result;
        try
        {
            var cands = BuildBreastPoolCandidates(pawn);
            if (cands.Count == 0) return result;
            cands.Sort((a, b) => a.ListIndex.CompareTo(b.ListIndex));
            int poolIdx = 0;
            for (int i = 0; i < cands.Count; i++)
            {
                bool isLeft = IsAnatomicallyLeftBreastPart(cands[i].Part);
                // 新模型：一条乳房子部位 = 一个池；每条池独占一个 PoolIndex。
                AppendNaturalSide(result, poolIdx, cands[i], isLeft);
                poolIdx++;
            }
        }
        catch (Exception ex)
        {
            LogDev(nameof(GetBreastPoolSideRows), ex);
            result.Clear();
        }
        return result;
    }

    /// <summary>部位 customLabel / Def 名可辨认为左乳。</summary>
    public static bool IsAnatomicallyLeftBreastPart(BodyPartRecord part) => PartNameLooksLeft(part);

    /// <summary>部位可辨认为右乳；未标注侧既不左也不右（不计入左/右汇总）。</summary>
    public static bool IsAnatomicallyRightBreastPart(BodyPartRecord part) => PartNameLooksRight(part);

    private static bool IsLateralBreastLeafPart(BodyPartRecord part)
    {
        if (part?.def?.defName == null) return false;
        string d = part.def.defName;
        return d == "Breast" || d == "MechBreast";
    }

    private static List<Cand> BuildBreastPoolCandidates(Pawn pawn)
    {
        var cands = new List<Cand>();
        var list = pawn.GetBreastListOrEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            var h = list[i];
            if (!IsBreastHediffForPool(h)) continue;
            if (h.Part == null || !IsLateralBreastLeafPart(h.Part)) continue;
            bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
            float vol = hasBs ? bs.volume : 0f;
            float w = hasBs ? bs.weight : 0f;
            bool usableSize = hasBs && (vol > 0f || w > 0f);
            float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
            if (baseCap <= PoolModelConstants.Epsilon) continue;
            float flow = GetBreastHediffFlowMultiplier(h);
            cands.Add(new Cand(i, h, h.Part, baseCap, flow));
        }

        return cands;
    }

    private static void AppendBreastPoolSnapshot(List<RjwBreastPoolSnapshot> result, Cand c, int poolIdx)
    {
        var h = c.H;
        int i = c.ListIndex;
        string poolKey = MakeStablePoolKey(h, i);
        bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
        float vol = hasBs ? bs.volume : 0f;
        float w = hasBs ? bs.weight : 0f;
        float cup = hasBs ? bs.cupSize : 0f;
        float band = hasBs ? bs.bandSize : 0f;
        bool usableSize = hasBs && (vol > 0f || w > 0f);
        float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
        float flow = GetBreastHediffFlowMultiplier(h);
        result.Add(new RjwBreastPoolSnapshot(
            listIndex: i,
            poolIndex: poolIdx,
            poolKey: poolKey,
            breastHediff: h,
            baseCapacityPerSide: baseCap,
            flowMultiplier: flow,
            breastVolumeLiters: vol,
            breastWeightKg: w,
            breastCupSize: cup,
            breastBandSize: band,
            hasBreastSize: usableSize));
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

    private static bool PartNameLooksLeft(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("左")) return true;
            if (part.customLabel.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool PartNameLooksRight(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("右")) return true;
            if (part.customLabel.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void AppendNaturalSide(List<RjwBreastPoolSideRow> result, int poolIdx, Cand c, bool isLeft)
    {
        string key = MakeStablePoolKey(c.H, c.ListIndex);
        if (string.IsNullOrEmpty(key)) return;
        result.Add(new RjwBreastPoolSideRow(poolIdx, key, c.H, c.BaseCap, c.Flow, isLeft));
    }
}

/// <summary>一条「侧」乳池行：与 <see cref="FluidPoolEntry"/> 一一对应。</summary>
public readonly struct RjwBreastPoolSideRow
{
    public int PoolIndex { get; }
    public string PoolKey { get; }
    public Hediff BreastHediff { get; }
    public float BaseCapacity { get; }
    public float FlowMultiplier { get; }
    /// <summary>仅解剖左为 true；解剖右与未标注为 false（与 <see cref="FluidPoolEntry.IsLeft"/> 一致）。</summary>
    public bool IsLeft { get; }

    public RjwBreastPoolSideRow(int poolIndex, string poolKey, Hediff breastHediff, float baseCapacity, float flowMultiplier, bool isLeft)
    {
        PoolIndex = poolIndex;
        PoolKey = poolKey ?? "";
        BreastHediff = breastHediff;
        BaseCapacity = baseCapacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
    }
}

/// <summary>
/// 一条 RJW 乳房 Hediff 在乳池经济中的只读快照（验收：阶段 1 乳房快照）。
/// ListIndex 为 GetBreastList 下标；PoolKey 为 <see cref="RjwBreastPoolEconomy.MakeStablePoolKey(Verse.Hediff,System.Int32)"/>（叶部位路径，含 customLabel 区分左右）。
/// PoolIndex：乳池序号（按遍历顺序递增）；当前模型每个乳房子部位独立一池。
/// </summary>
public readonly struct RjwBreastPoolSnapshot
{
    public int ListIndex { get; }
    public int PoolIndex { get; }
    public string PoolKey { get; }
    public Hediff BreastHediff { get; }
    public float BaseCapacityPerSide { get; }
    public float FlowMultiplier { get; }
    public float BreastVolumeLiters { get; }
    public float BreastWeightKg { get; }
    public float BreastCupSize { get; }
    public float BreastBandSize { get; }
    public bool HasBreastSize { get; }

    public RjwBreastPoolSnapshot(
        int listIndex,
        int poolIndex,
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
        PoolIndex = poolIndex;
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
