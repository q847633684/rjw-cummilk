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
/// 乳池布局固定为虚拟左·右（<c>Breast</c>/<c>MechBreast</c> 叶、<c>基键_L/_R</c>）。
/// <see cref="RjwBreastPoolSnapshot"/> 为「乳房快照」SSOT：容量/流速/UI/RJW Severity 同步均应对齐此结构中的量。
/// </summary>
public static class RjwBreastPoolEconomy
{
    /// <summary>虚拟左右储奶后缀：每条 RJW 乳房 Hediff 对应 <c>base_L</c> 与 <c>base_R</c> 两格（与 96607df 前行为一致）。<c>base</c> 为 <see cref="MakeStablePoolKey"/>。</summary>
    public const string VirtualBreastLeftStorageSuffix = "_L";

    /// <summary>见 <see cref="VirtualBreastLeftStorageSuffix"/>。</summary>
    public const string VirtualBreastRightStorageSuffix = "_R";

    /// <summary>稳定基键 + 虚拟左/右储奶后缀。</summary>
    public static string AppendVirtualBreastStorageSuffix(string baseStableKey, bool leftVirtual) =>
        (baseStableKey ?? "") + (leftVirtual ? VirtualBreastLeftStorageSuffix : VirtualBreastRightStorageSuffix);

    /// <summary>若为 <c>…_L</c>/<c>…_R</c> 储奶键则剥离后缀得到 <see cref="MakeStablePoolKey"/> 基键。</summary>
    public static bool TryStripVirtualBreastStorageSuffix(string storageKey, out string baseStableKey)
    {
        baseStableKey = storageKey;
        if (string.IsNullOrEmpty(storageKey))
            return false;
        if (storageKey.EndsWith(VirtualBreastLeftStorageSuffix, StringComparison.Ordinal))
        {
            baseStableKey = storageKey.Substring(0, storageKey.Length - VirtualBreastLeftStorageSuffix.Length);
            return true;
        }

        if (storageKey.EndsWith(VirtualBreastRightStorageSuffix, StringComparison.Ordinal))
        {
            baseStableKey = storageKey.Substring(0, storageKey.Length - VirtualBreastRightStorageSuffix.Length);
            return true;
        }

        return false;
    }

    /// <summary><see cref="RjwBreastPoolSnapshot.PoolKey"/> 等基键对应的两侧容量之和（<c>base_L</c>+<c>base_R</c>）。</summary>
    public static float CapacitySumForStableBreastBaseKey(List<FluidPoolEntry> ent, string baseStableKey)
    {
        if (ent == null || string.IsNullOrEmpty(baseStableKey)) return 0f;
        return CapacityForPoolKey(ent, AppendVirtualBreastStorageSuffix(baseStableKey, true))
            + CapacityForPoolKey(ent, AppendVirtualBreastStorageSuffix(baseStableKey, false));
    }

    /// <summary>组织适应与 <see cref="CompEquallyMilkable"/> 的 maxFullness 基底：每快照乳房 2 倍 <see cref="RjwBreastPoolSnapshot.BaseCapacityPerSide"/>（<c>_L</c>+<c>_R</c>）；异于解剖左+右各行只计一次之和，避免双池后分母偏小。</summary>
    public static float GetTotalDualPoolBaseCapacityBeforeAdaptation(Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive) return 0f;
        float sum = 0f;
        try
        {
            var snaps = GetBreastPoolSnapshots(pawn);
            for (int i = 0; i < snaps.Count; i++)
                sum += 2f * Mathf.Max(0f, snaps[i].BaseCapacityPerSide);
        }
        catch (Exception ex)
        {
            LogDev(nameof(GetTotalDualPoolBaseCapacityBeforeAdaptation), ex);
        }

        return sum;
    }

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

    /// <summary>侧行对应乳池键：有 RJW 乳房列表下标时用稳定路径键。</summary>
    public static string TryStablePoolKeyForSideRow(RjwBreastPoolSideRow row)
    {
        if (row.BreastHediff != null && row.SourceBreastListIndex >= 0)
            return MakeStablePoolKey(row.BreastHediff, row.SourceBreastListIndex);
        return null;
    }

    /// <summary>当前泌乳乳房列表的稳定键签名（条数不变时 Part/路径变化也会变），供条目缓存失效检测。</summary>
    public static string BuildBreastListPoolKeySignature(Pawn pawn)
    {
        if (pawn == null) return "";
        var sb = new StringBuilder();
        var list = pawn.GetBreastListOrEmpty();
        if (list.Count == 0) return sb.ToString();
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

    /// <summary>当前小人、在启用 RJW 胸池时的乳房快照（虚拟左·右）。</summary>
    public static List<RjwBreastPoolSnapshot> GetBreastPoolSnapshots(Pawn pawn) =>
        SnapshotsVirtualLeftRight(pawn);

    /// <summary>虚拟左·右拓扑：快照仅来自 <see cref="GatherLateralBreastLeafCands"/>。</summary>
    internal static List<RjwBreastPoolSnapshot> SnapshotsVirtualLeftRight(Pawn pawn)
    {
        var result = new List<RjwBreastPoolSnapshot>();
        var cands = GatherLateralBreastLeafCands(pawn);
        if (cands.Count == 0) return result;
        cands.Sort((a, b) => a.ListIndex.CompareTo(b.ListIndex));
        for (int i = 0; i < cands.Count; i++)
            AppendBreastPoolSnapshot(result, cands[i], i);
        return result;
    }

    /// <summary>
    /// 每条可泌乳乳房一条解剖行；储奶为每条乳房 Hediff 的 <c>稳定基键_L/_R</c> 双格（虚拟左右池）。
    /// 含 <c>Breast</c>/<c>MechBreast</c> 叶及 RJW 常见的 <c>Chest</c>/无部位；容量为 0 不进列表。
    /// 同 tick、同键签名时由 <see cref="BreastPoolSideRowsCache"/> 缓存，避免重复构建。
    /// </summary>
    public static List<RjwBreastPoolSideRow> GetBreastPoolSideRows(Pawn pawn) =>
        pawn == null || !ModIntegrationGates.RjwModActive
            ? new List<RjwBreastPoolSideRow>()
            : BreastPoolSideRowsCache.GetCached(pawn, BuildBreastPoolSideRowsUncached);

    private static List<RjwBreastPoolSideRow> BuildBreastPoolSideRowsUncached(Pawn pawn) =>
        BuildVirtualLeftRightSideRowsUncached(pawn);

    /// <summary>虚拟左·右：候选来自 <see cref="GatherLateralBreastLeafCands"/>；胸位/无部位行标为左列；叶上行仅解剖左为左（与 <see cref="PawnMilkPoolExtensions.GetBreastCapacityFactors"/> 一致）。</summary>
    internal static List<RjwBreastPoolSideRow> BuildVirtualLeftRightSideRowsUncached(Pawn pawn)
    {
        var result = new List<RjwBreastPoolSideRow>();
        var cands = GatherLateralBreastLeafCands(pawn);
        if (cands.Count == 0) return result;
        cands.Sort((a, b) => a.ListIndex.CompareTo(b.ListIndex));
        int poolIdx = 0;
        for (int i = 0; i < cands.Count; i++)
        {
            BodyPartRecord p = cands[i].Part;
            // 胸位/无部位：与历史 ChestUnified 一致，侧行标为左列汇总，避免容量不进左/右因子。
            bool isLeft = IsChestOrUnpartedBreastSlot(p) || IsAnatomicallyLeftBreastPart(p);
            AppendNaturalSide(result, poolIdx, cands[i], isLeft);
            poolIdx++;
        }

        return result;
    }

    /// <summary>部位 customLabel / Def 名可辨认为左乳（与 <see cref="BodyPartLaterality.PartNameLooksLeft"/> 一致）。</summary>
    public static bool IsAnatomicallyLeftBreastPart(BodyPartRecord part) => BodyPartLaterality.PartNameLooksLeft(part);

    /// <summary>部位可辨认为右乳；未标注侧既不左也不右（不计入左/右汇总）。</summary>
    public static bool IsAnatomicallyRightBreastPart(BodyPartRecord part) => BodyPartLaterality.PartNameLooksRight(part);

    internal static bool IsLateralBreastLeafPart(BodyPartRecord part)
    {
        if (part?.def?.defName == null) return false;
        string d = part.def.defName;
        return d == "Breast" || d == "MechBreast";
    }

    /// <summary>RJW 常见：乳房 SexPart 挂在 <c>Chest</c> 或无 <see cref="Hediff.Part"/>，与左右叶并列参与虚拟池。</summary>
    public static bool IsChestOrUnpartedBreastSlot(BodyPartRecord part) =>
        part == null || part.def?.defName == "Chest";

    /// <summary>侧向乳房叶候选：<c>Breast</c>/<c>MechBreast</c> 叶，或胸位/无部位；容量&gt;0。</summary>
    private static List<Cand> GatherLateralBreastLeafCands(Pawn pawn)
    {
        var cands = new List<Cand>();
        var list = pawn.GetBreastListOrEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            var h = list[i];
            if (!IsBreastHediffForPool(h)) continue;
            BodyPartRecord p = h.Part;
            if (!IsChestOrUnpartedBreastSlot(p) && !IsLateralBreastLeafPart(p)) continue;
            bool hasBs = PartSizeCalculator.TryGetBreastSize(h, out var bs);
            float vol = hasBs ? bs.volume : 0f;
            float w = hasBs ? bs.weight : 0f;
            bool usableSize = hasBs && (vol > 0f || w > 0f);
            float baseCap = ComputeBaseCapacityPerSide(h, vol, w, usableSize);
            if (baseCap <= PoolModelConstants.Epsilon) continue;
            float flow = GetBreastHediffFlowMultiplier(h);
            cands.Add(new Cand(i, h, p, baseCap, flow));
        }

        return cands;
    }

    /// <summary>虚拟左·右拓扑：由侧行构建池条目（组织适应摊入）。侧行经 <see cref="GetBreastPoolSideRows"/> 与同 tick 缓存对齐，避免与 <see cref="MilkCum.Fluids.Lactation.Comps.CompEquallyMilkable"/> 刷新条目时重复构建。</summary>
    internal static List<FluidPoolEntry> BuildVirtualLeftRightBreastPoolEntries(Pawn pawn) =>
        BuildBreastPoolEntriesFromSideRows(pawn, GetBreastPoolSideRows(pawn));

    /// <summary>由解剖侧行生成 <c>基键_L/_R</c> 条目并摊入组织适应。</summary>
    internal static List<FluidPoolEntry> BuildBreastPoolEntriesFromSideRows(Pawn pawn, List<RjwBreastPoolSideRow> rows)
    {
        var result = new List<FluidPoolEntry>();
        if (pawn == null || rows == null || rows.Count == 0) return result;
        try
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.BreastHediff == null || r.SourceBreastListIndex < 0) continue;
                string baseKey = MakeStablePoolKey(r.BreastHediff, r.SourceBreastListIndex);
                BodyPartRecord part = r.BreastHediff.Part;
                int pi = r.PoolIndex * 2;
                result.Add(new FluidPoolEntry(
                    AppendVirtualBreastStorageSuffix(baseKey, true),
                    FluidSiteKind.BreastLeft,
                    r.BaseCapacity,
                    r.FlowMultiplier,
                    true,
                    pi,
                    part));
                result.Add(new FluidPoolEntry(
                    AppendVirtualBreastStorageSuffix(baseKey, false),
                    FluidSiteKind.BreastRight,
                    r.BaseCapacity,
                    r.FlowMultiplier,
                    false,
                    pi + 1,
                    part));
            }

            PawnMilkPoolExtensions.ApplyCapacityAdaptationToBreastEntries(pawn, result);
        }
        catch (Exception ex)
        {
            LogDev(nameof(BuildBreastPoolEntriesFromSideRows), ex);
            result.Clear();
        }

        return result;
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
        float baseCap = c.BaseCap;
        float flow = c.Flow;
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

    private static void AppendNaturalSide(List<RjwBreastPoolSideRow> result, int poolIdx, Cand c, bool isLeft) =>
        result.Add(new RjwBreastPoolSideRow(poolIdx, c.H, c.BaseCap, c.Flow, isLeft, c.ListIndex));

    /// <summary>该解剖行涉及的储奶键：<c>稳定基键_L</c> 与 <c>_R</c>。</summary>
    public static void AddVirtualBreastStorageKeysForSideRow(RjwBreastPoolSideRow row, HashSet<string> dest)
    {
        if (dest == null) return;
        string stable = TryStablePoolKeyForSideRow(row);
        if (string.IsNullOrEmpty(stable)) return;
        dest.Add(AppendVirtualBreastStorageSuffix(stable, true));
        dest.Add(AppendVirtualBreastStorageSuffix(stable, false));
    }

    /// <summary>单解剖行对应的稳定基键（无 <c>_L/_R</c>）；存奶子键为 <see cref="AppendVirtualBreastStorageSuffix"/>。</summary>
    public static string GetVirtualBreastStorageKeyForSideRow(RjwBreastPoolSideRow row) =>
        TryStablePoolKeyForSideRow(row);

    /// <summary>高潮产液：<c>base_L</c> 与 <c>base_R</c> 各追加一整份 <paramref name="fullUnit"/>（与 96607df 及 RJW 侧「每侧各一份」手感一致；总行液量约为各半方案的两倍）。</summary>
    public static void AppendOrgasmMilkTargetsForSideRow(
        RjwBreastPoolSideRow row,
        float fullUnit,
        List<FluidPoolEntry> entries,
        List<(string key, float addAmount, float cap)> toAdd)
    {
        if (entries == null || toAdd == null || fullUnit <= 0f) return;
        string stable = TryStablePoolKeyForSideRow(row);
        if (string.IsNullOrEmpty(stable)) return;
        string kL = AppendVirtualBreastStorageSuffix(stable, true);
        string kR = AppendVirtualBreastStorageSuffix(stable, false);
        float cL = CapacityForPoolKey(entries, kL);
        float cR = CapacityForPoolKey(entries, kR);
        if (cL > PoolModelConstants.Epsilon) toAdd.Add((kL, fullUnit, cL));
        if (cR > PoolModelConstants.Epsilon) toAdd.Add((kR, fullUnit, cR));
    }

    /// <summary>按池键匹配条目容量（支持 <see cref="FluidSiteKind.BreastLeft"/>/<see cref="FluidSiteKind.BreastRight"/> 及每叶自定义键）。</summary>
    public static float CapacityForPoolKey(List<FluidPoolEntry> ent, string key)
    {
        if (ent == null || string.IsNullOrEmpty(key)) return 0f;
        for (int i = 0; i < ent.Count; i++)
            if (ent[i].Key == key) return ent[i].Capacity;
        return 0f;
    }
}

/// <summary>一条解剖「叶」乳行；虚拟左/右与每叶拓扑下每行对应一颗乳房，乳池条目由稳定键区分多叶。</summary>
public readonly struct RjwBreastPoolSideRow
{
    public int PoolIndex { get; }
    public Hediff BreastHediff { get; }
    public float BaseCapacity { get; }
    public float FlowMultiplier { get; }
    /// <summary>仅解剖左为 true；未标注侧为 false（不参与左/右基容量汇总，<see cref="FluidSiteKind"/> 为 None）。</summary>
    public bool IsLeft { get; }
    /// <summary>RJW 乳房列表下标；每叶独立池键与稳定路径键生成一致。</summary>
    public int SourceBreastListIndex { get; }

    public RjwBreastPoolSideRow(int poolIndex, Hediff breastHediff, float baseCapacity, float flowMultiplier, bool isLeft, int sourceBreastListIndex = -1)
    {
        PoolIndex = poolIndex;
        BreastHediff = breastHediff;
        BaseCapacity = baseCapacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
        SourceBreastListIndex = sourceBreastListIndex;
    }
}

/// <summary>
/// 一条 RJW 乳房 Hediff 在乳池经济中的只读快照（验收：阶段 1 乳房快照）。
/// ListIndex 为 GetBreastList 下标；PoolKey 为 <see cref="RjwBreastPoolEconomy.MakeStablePoolKey(Verse.Hediff,System.Int32)"/> 稳定基键（无 <c>_L/_R</c>）；储奶为 <c>PoolKey_L</c>/<c>PoolKey_R</c>。
/// PoolIndex：解剖行序号；储奶格为每行两键。
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
