using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池扣量：手挤/吸奶/机器挤的 Drain 与按比例缩减。见 Docs/泌乳系统逻辑图、ADR-003-选侧先左。</summary>
public partial class CompEquallyMilkable
{
    private static void AccumulateDrainByKey(Dictionary<string, float> map, string key, float amount)
    {
        if (map == null || amount <= 0f || string.IsNullOrEmpty(key)) return;
        map.TryGetValue(key, out float x);
        map[key] = x + amount;
    }

    /// <summary>
    /// 扣奶顺序：满度高者优先；差在 <see cref="PoolModelConstants.FloatDustEpsilon"/> 内为平局则 <see cref="PreferLeftWhenEqual"/>（解剖左）；仍平则优先解剖右；再比条目下标。
    /// 返回值 &lt; 0 表示 <paramref name="ia"/> 应先于 <paramref name="ib"/>（与降序 Sort 一致）。
    /// </summary>
    private int CompareDrainEntryOrder(int ia, int ib, List<FluidPoolEntry> entries)
    {
        if (entries == null || ia < 0 || ib < 0 || ia >= entries.Count || ib >= entries.Count) return ia.CompareTo(ib);
        float fa = GetFullnessForKey(entries[ia].Key);
        float fb = GetFullnessForKey(entries[ib].Key);
        if (fa > fb + PoolModelConstants.FloatDustEpsilon) return -1;
        if (fb > fa + PoolModelConstants.FloatDustEpsilon) return 1;
        if (PreferLeftWhenEqual)
        {
            if (entries[ia].IsLeft && !entries[ib].IsLeft) return -1;
            if (entries[ib].IsLeft && !entries[ia].IsLeft) return 1;
        }

        bool ra = RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(entries[ia].SourcePart);
        bool rb = RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(entries[ib].SourcePart);
        if (ra && !rb) return -1;
        if (rb && !ra) return 1;

        return ia.CompareTo(ib);
    }
    /// <summary>
    /// 吸奶/挤奶时从池中扣量。
    /// singleSideOnly=false（默认）：按<strong>每条池单 key 满度</strong>全局从高到低依次扣，直到扣满 amount；满度相同则先左（ADR-003）。用于手挤奶（与吸奶同属「按单池满度」规则，手挤可连续扣多条）。
    /// singleSideOnly=true：只从当前全局最满的一条池扣，最多扣 amount。用于吸奶。见 Docs/泌乳系统逻辑图；ADR-003。
    /// </summary>
    /// <param name="amount">要扣的池单位量（与 Charge/Fullness 同单位）</param>
    /// <param name="drainedKeys">若非 null，会填入本次被扣量的池侧 key（用于按侧加喷乳反射刺激）</param>
    /// <param name="singleSideOnly">true=只从当前最满的一侧扣（吸奶）；false=按顺序多侧扣至 amount（手挤奶）</param>
    /// <returns>实际扣掉的量</returns>
    public float DrainForConsume(float amount, List<string> drainedKeys = null, bool singleSideOnly = false)
    {
        if (amount <= 0f || Pawn == null) return 0f;
        var entries = GetCachedEntries();
        if (entries.Count == 0)
        {
            SyncBaseFullness();
            return 0f;
        }
        if (singleSideOnly)
        {
            int idx = GetFirstDrainSideIndex();
            if (idx < 0 || idx >= entries.Count) { SyncBaseFullness(); return 0f; }
            var maxTakePerSide = new List<float>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
                maxTakePerSide.Add(i == idx ? amount : 0f);
            return DrainForConsumeByRates(maxTakePerSide, amount, drainedKeys, "DrainSingleSide", idx);
        }
        int tick = Find.TickManager.TicksGame;
        float fullnessBeforeTotal = Fullness;
        breastFullness ??= new Dictionary<string, float>();
        var drainedByKey = new Dictionary<string, float>();
        float remaining = amount;
        var handOrder = new List<int>(entries.Count);
        SortHandDrainEntryIndicesDescending(handOrder, entries);
        for (int o = 0; o < handOrder.Count && remaining > PoolModelConstants.Epsilon; o++)
        {
            int i = handOrder[o];
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float f = GetFullnessForKey(e.Key);
            float take = Mathf.Min(remaining, f);
            if (take <= 0f) continue;
            breastFullness[e.Key] = Mathf.Max(0f, f - take);
            drainedKeys?.Add(e.Key);
            AccumulateDrainByKey(drainedByKey, e.Key, take);
            remaining -= take;
        }
        if (remaining > PoolModelConstants.Epsilon && remaining < PoolModelConstants.FloatDustEpsilon)
        {
            foreach (var e in entries)
            {
                if (remaining <= PoolModelConstants.Epsilon || string.IsNullOrEmpty(e.Key)) break;
                float f = GetFullnessForKey(e.Key);
                if (f <= 0f) continue;
                float take = Mathf.Min(remaining, f);
                breastFullness[e.Key] = Mathf.Max(0f, f - take);
                if (drainedKeys != null && !drainedKeys.Contains(e.Key)) drainedKeys.Add(e.Key);
                AccumulateDrainByKey(drainedByKey, e.Key, take);
                remaining -= take;
            }
        }
        SyncBaseFullness();
        float drained = amount - remaining;
        if (drained > 0f)
            NotifyInflammationDrain(drainedByKey);
        if (MilkCumSettings.milkingActionLog && Pawn != null && drained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            MilkCumSettings.LactationLog($"[MilkCum][INFO][Milking] pawn={Pawn.LabelShort} tick={tick} mode=DrainForConsume amountReq={amount:F3} drained={drained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}");
        }
        return drained;
    }

    /// <summary>手挤：条目下标按单 key 满度降序；差在 FloatDust 内视为平局则先左；再比下标。</summary>
    private void SortHandDrainEntryIndicesDescending(List<int> order, List<FluidPoolEntry> entries)
    {
        order.Clear();
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!string.IsNullOrEmpty(entries[i].Key))
                order.Add(i);
        }

        order.Sort((ia, ib) => CompareDrainEntryOrder(ia, ib, entries));
    }

    /// <summary>
    /// 统一扣量逻辑：每侧有「本侧最多扣」上限 maxTakePerSide，总扣量不超过 remainingCap；若各侧拟扣之和超过 cap 则按比例缩减。吸奶=单侧有上限，机器挤=多侧并行。调用前需确保 entries 已取、非空。
    /// </summary>
    private float DrainForConsumeByRates(IList<float> maxTakePerSide, float remainingCap, List<string> drainedKeys, string logMode, int? singleDrainSideIndex)
    {
        if (maxTakePerSide == null || maxTakePerSide.Count == 0 || remainingCap <= 0f) return 0f;
        int tick = Find.TickManager.TicksGame;
        float fullnessBeforeTotal = Fullness;
        breastFullness ??= new Dictionary<string, float>();
        var entries = GetCachedEntries();
        if (entries.Count == 0)
        {
            SyncBaseFullness();
            return 0f;
        }
        var drainedByKey = new Dictionary<string, float>();
        float totalWouldTake = 0f;
        var takes = new List<float>(entries.Count);
        for (int i = 0; i < entries.Count && i < maxTakePerSide.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) { takes.Add(0f); continue; }
            float f = GetFullnessForKey(e.Key);
            float take = Mathf.Min(maxTakePerSide[i], f);
            takes.Add(take);
            totalWouldTake += take;
        }
        while (takes.Count < entries.Count) takes.Add(0f);
        float factor = totalWouldTake > remainingCap && totalWouldTake > PoolModelConstants.Epsilon ? remainingCap / totalWouldTake : 1f;
        float totalDrained = 0f;
        for (int i = 0; i < entries.Count && i < takes.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float take = takes[i] * factor;
            if (take <= 0f) continue;
            float cur = GetFullnessForKey(e.Key);
            take = Mathf.Min(take, cur);
            if (take > 0f)
            {
                breastFullness[e.Key] = Mathf.Max(0f, cur - take);
                drainedKeys?.Add(e.Key);
                AccumulateDrainByKey(drainedByKey, e.Key, take);
                totalDrained += take;
            }
        }
        float remainingRequest = remainingCap - totalDrained;
        if (remainingRequest > PoolModelConstants.Epsilon && remainingRequest < PoolModelConstants.FloatDustEpsilon)
        {
            for (int i = 0; i < entries.Count && remainingRequest > PoolModelConstants.Epsilon; i++)
            {
                var e = entries[i];
                if (string.IsNullOrEmpty(e.Key)) continue;
                float f = GetFullnessForKey(e.Key);
                if (f <= 0f) continue;
                float take = Mathf.Min(remainingRequest, f);
                breastFullness[e.Key] = Mathf.Max(0f, f - take);
                drainedKeys?.Add(e.Key);
                AccumulateDrainByKey(drainedByKey, e.Key, take);
                totalDrained += take;
                remainingRequest -= take;
            }
        }
        SyncBaseFullness();
        if (totalDrained > 0f)
            NotifyInflammationDrain(drainedByKey);
        if (MilkCumSettings.milkingActionLog && Pawn != null && totalDrained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            string sideKeyPart = singleDrainSideIndex.HasValue && singleDrainSideIndex.Value >= 0 && singleDrainSideIndex.Value < entries.Count
                ? $" sideKey={entries[singleDrainSideIndex.Value].Key}"
                : "";
            if (logMode == "DrainSingleSide")
                MilkCumSettings.LactationLog($"[MilkCum][INFO][Milking] pawn={Pawn.LabelShort} tick={tick} mode=DrainSingleSide amountReq={remainingCap:F3} drained={totalDrained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}{sideKeyPart}");
            else
                MilkCumSettings.LactationLog($"[MilkCum][INFO][Milking] pawn={Pawn.LabelShort} tick={tick} mode=DrainParallel cap={remainingCap:F3} drained={totalDrained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}");
        }
        return totalDrained;
    }

    /// <summary>
    /// 机器挤奶专用：每侧按「该侧流速」独立并行扣量；总扣量不超过 remainingCap。每侧本 tick 最多扣 ratePerSidePerTick[i]（与 GetCachedEntries 顺序一致），若各侧拟扣量之和超过 remainingCap 则按比例缩减。用于左 2.5 秒、右 1 秒同步进行，总耗时由最慢的一侧决定。
    /// </summary>
    public float DrainForConsumeParallel(IList<float> ratePerSidePerTick, float remainingCap, List<string> drainedKeys = null)
    {
        if (ratePerSidePerTick == null || ratePerSidePerTick.Count == 0 || remainingCap <= 0f || Pawn == null) return 0f;
        return DrainForConsumeByRates(ratePerSidePerTick, remainingCap, drainedKeys, "DrainParallel", null);
    }
}
