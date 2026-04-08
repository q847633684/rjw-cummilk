using System.Collections.Generic;
using System.Text;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池扣量：手挤/吸奶/机器挤的 Drain 与按比例缩减。见 记忆库/docs/泌乳系统逻辑图、ADR-003-选侧先左。</summary>
public partial class CompEquallyMilkable
{
    /// <summary>扣量导通系数：压力与导管阻力共同决定单位时间可抽取比例。</summary>
    private float GetDuctTakeFactor(FluidPoolEntry entry, FluidPoolNetwork network, bool isMachine)
    {
        if (string.IsNullOrEmpty(entry.Key)) return 1f;
        float pressure = network?.GetPressureRatio01(entry.Key) ?? 0f;
        float outlet = network?.GetOutletHopFactor(entry.Key, MilkCumSettings.ductHopPenaltyPerEdge) ?? 1f;
        float inflammation = MilkPoolInflowSimulator.GetNormalizedInflammation01(Pawn?.LactatingHediffComp(), entry.Key);
        float resistance = 1f + inflammation * (isMachine ? MilkCumSettings.ductDrainInflammationResistanceMachine : MilkCumSettings.ductDrainInflammationResistanceManual);
        float suction = isMachine ? MilkCumSettings.ductMachineSuctionBonus : 1f;
        float pressureTerm = MilkCumSettings.ductDrainPressureBase + MilkCumSettings.ductDrainPressureScale * pressure;
        float factor = suction * outlet * pressureTerm / Mathf.Max(MilkCumSettings.ductConductanceMin, resistance);
        return Mathf.Clamp(factor, MilkCumSettings.ductConductanceMin, MilkCumSettings.ductConductanceMax);
    }

    /// <summary>DevMode 导管模型调试：输出每池压力/出口因子/导通系数（手动与机器）。</summary>
    internal string BuildDuctDebugString(int maxRows = 8)
    {
        var entries = GetResolvedBreastPoolEntries();
        if (entries == null || entries.Count == 0) return "[MilkCum.Duct] no pools";
        var network = FluidPoolNetwork.Build(entries, breastFullness);
        var sb = new StringBuilder();
        sb.Append("[MilkCum.Duct]");
        float sumInflow = 0f;
        float sumDrainManual = 0f;
        float sumDrainMachine = 0f;
        int validCount = 0;
        float worstInflow = float.MaxValue;
        string worstKey = null;
        int rows = 0;
        var lact = Pawn?.LactatingHediffComp();
        for (int i = 0; i < entries.Count; i++)
        {
            if (rows >= Mathf.Max(1, maxRows)) break;
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float pressure = network.GetPressureRatio01(e.Key);
            float outlet = network.GetOutletHopFactor(e.Key, MilkCumSettings.ductHopPenaltyPerEdge);
            float inflammation = MilkPoolInflowSimulator.GetNormalizedInflammation01(lact, e.Key);
            float inflowRes = 1f + inflammation * MilkCumSettings.ductInflowInflammationResistance;
            float inflowCond = Mathf.Clamp(outlet / Mathf.Max(MilkCumSettings.ductConductanceMin, inflowRes), MilkCumSettings.ductConductanceMin, MilkCumSettings.ductConductanceMax);
            float drainManual = GetDuctTakeFactor(e, network, isMachine: false);
            float drainMachine = GetDuctTakeFactor(e, network, isMachine: true);
            sumInflow += inflowCond;
            sumDrainManual += drainManual;
            sumDrainMachine += drainMachine;
            validCount++;
            if (inflowCond < worstInflow)
            {
                worstInflow = inflowCond;
                worstKey = e.Key;
            }
            sb.Append("\n  ").Append(e.Key)
              .Append(" p=").Append(pressure.ToString("F2"))
              .Append(" hop=").Append(outlet.ToString("F2"))
              .Append(" i=").Append(inflammation.ToString("F2"))
              .Append(" in=").Append(inflowCond.ToString("F2"))
              .Append(" dM=").Append(drainManual.ToString("F2"))
              .Append(" dX=").Append(drainMachine.ToString("F2"));
            rows++;
        }
        if (validCount > 0)
        {
            sb.Append("\n  [summary] n=").Append(validCount)
              .Append(" avgIn=").Append((sumInflow / validCount).ToString("F2"))
              .Append(" avgDM=").Append((sumDrainManual / validCount).ToString("F2"))
              .Append(" avgDX=").Append((sumDrainMachine / validCount).ToString("F2"))
              .Append(" worstIn=").Append(worstKey ?? "-")
              .Append("(").Append(worstInflow.ToString("F2")).Append(")");
        }
        return sb.ToString();
    }

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
            if (entries[ia].Site == FluidSiteKind.BreastLeft && entries[ib].Site != FluidSiteKind.BreastLeft) return -1;
            if (entries[ib].Site == FluidSiteKind.BreastLeft && entries[ia].Site != FluidSiteKind.BreastLeft) return 1;
        }

        bool ra = entries[ia].Site == FluidSiteKind.BreastRight;
        bool rb = entries[ib].Site == FluidSiteKind.BreastRight;
        if (ra && !rb) return -1;
        if (rb && !ra) return 1;

        return ia.CompareTo(ib);
    }
    /// <summary>
    /// 吸奶/挤奶时从池中扣量。
    /// singleSideOnly=false（默认）：按<strong>每条池单 key 满度</strong>全局从高到低依次扣，直到扣满 amount；满度相同则先左（ADR-003）。用于手挤奶（与吸奶同属「按单池满度」规则，手挤可连续扣多条）。
    /// singleSideOnly=true：只从当前全局最满的一条池扣，最多扣 amount。用于吸奶。见 记忆库/docs/泌乳系统逻辑图；ADR-003。
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
        var network = FluidPoolNetwork.Build(entries, breastFullness);
        for (int o = 0; o < handOrder.Count && remaining > PoolModelConstants.Epsilon; o++)
        {
            int i = handOrder[o];
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            float f = GetFullnessForKey(e.Key);
            float take = Mathf.Min(remaining, f) * GetDuctTakeFactor(e, network, isMachine: false);
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
        {
            NotifyInflammationDrain(drainedByKey);
            TriggerInflowEventBurst();
        }
        if (MilkCumSettings.milkingActionLog && Pawn != null && drained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            MilkCumSettings.MilkingActionLogMessage($"pawn={Pawn.LabelShort} tick={tick} mode=DrainForConsume amountReq={amount:F3} drained={drained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}");
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
        var network = FluidPoolNetwork.Build(entries, breastFullness);
        bool isMachine = logMode == "DrainParallel";
        float totalWouldTake = 0f;
        var takes = new List<float>(entries.Count);
        for (int i = 0; i < entries.Count && i < maxTakePerSide.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) { takes.Add(0f); continue; }
            float f = GetFullnessForKey(e.Key);
            float take = Mathf.Min(maxTakePerSide[i], f) * GetDuctTakeFactor(e, network, isMachine);
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
        {
            NotifyInflammationDrain(drainedByKey);
            TriggerInflowEventBurst();
        }
        if (MilkCumSettings.milkingActionLog && Pawn != null && totalDrained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            string sideKeyPart = singleDrainSideIndex.HasValue && singleDrainSideIndex.Value >= 0 && singleDrainSideIndex.Value < entries.Count
                ? $" sideKey={entries[singleDrainSideIndex.Value].Key}"
                : "";
            if (logMode == "DrainSingleSide")
                MilkCumSettings.MilkingActionLogMessage($"pawn={Pawn.LabelShort} tick={tick} mode=DrainSingleSide amountReq={remainingCap:F3} drained={totalDrained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}{sideKeyPart}");
            else
                MilkCumSettings.MilkingActionLogMessage($"pawn={Pawn.LabelShort} tick={tick} mode=DrainParallel cap={remainingCap:F3} drained={totalDrained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}");
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
