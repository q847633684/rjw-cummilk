using System.Collections.Generic;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池扣量：手挤/吸奶/机器挤的 Drain 与按比例缩减。见 Docs/泌乳系统逻辑图、ADR-003-选侧先左。</summary>
public partial class CompEquallyMilkable
{
    /// <summary>
    /// 吸奶/挤奶时从池中扣量。
    /// singleSideOnly=false（默认）：按「哪对最满」优先，同对内先扣较满的一侧，相同时先左，直到扣满 amount；含浮点余量吸收。用于手挤奶。
    /// singleSideOnly=true：只从「当前最满的一侧」扣，最多扣 amount。用于吸奶（一口只吸一侧）。见 Docs/泌乳系统逻辑图；ADR-003-选侧先左。
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
            SyncLeftRightFromBreastFullness();
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
        float remaining = amount;
        var pairGroups = BuildPairGroupsByFullnessDescending(entries);
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (remaining <= 0f) break;
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftF = GetFullnessForKey(leftE.Key);
                float rightF = GetFullnessForKey(rightE.Key);
                bool preferLeft = true;
                bool drainLeftFirst = leftF > rightF || (Mathf.Approximately(leftF, rightF) && preferLeft);
                string firstKey = drainLeftFirst ? leftE.Key : rightE.Key;
                string secondKey = drainLeftFirst ? rightE.Key : leftE.Key;
                float firstF = drainLeftFirst ? leftF : rightF;
                float secondF = drainLeftFirst ? rightF : leftF;
                float take1 = Mathf.Min(remaining, firstF);
                if (take1 > 0f)
                {
                    breastFullness[firstKey] = Mathf.Max(0f, firstF - take1);
                    drainedKeys?.Add(firstKey);
                    remaining -= take1;
                }
                if (remaining > 0f && secondF > 0f)
                {
                    float take2 = Mathf.Min(remaining, secondF);
                    breastFullness[secondKey] = Mathf.Max(0f, secondF - take2);
                    drainedKeys?.Add(secondKey);
                    remaining -= take2;
                }
            }
            else
            {
                for (int j = 0; j < list.Count; j++)
                {
                    var e = list[j];
                    if (remaining <= 0f || string.IsNullOrEmpty(e.Key)) continue;
                    float f = GetFullnessForKey(e.Key);
                    float take = Mathf.Min(remaining, f);
                    if (take > 0f)
                    {
                        breastFullness[e.Key] = Mathf.Max(0f, f - take);
                        drainedKeys?.Add(e.Key);
                        remaining -= take;
                    }
                }
            }
        }
        const float floatDustEpsilon = 0.001f;
        if (remaining > 1E-6f && remaining < floatDustEpsilon)
        {
            foreach (var e in entries)
            {
                if (remaining <= 1E-6f || string.IsNullOrEmpty(e.Key)) break;
                float f = GetFullnessForKey(e.Key);
                if (f <= 0f) continue;
                float take = Mathf.Min(remaining, f);
                breastFullness[e.Key] = Mathf.Max(0f, f - take);
                if (drainedKeys != null && !drainedKeys.Contains(e.Key)) drainedKeys.Add(e.Key);
                remaining -= take;
            }
        }
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
        float drained = amount - remaining;
        if (MilkCumSettings.milkingActionLog && Pawn != null && drained > 0f)
        {
            float fullnessAfterTotal = Fullness;
            MilkCumSettings.LactationLog($"[MilkCum][INFO][Milking] pawn={Pawn.LabelShort} tick={tick} mode=DrainForConsume amountReq={amount:F3} drained={drained:F3} fullnessBefore={fullnessBeforeTotal:F3} fullnessAfter={fullnessAfterTotal:F3}");
        }
        return drained;
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
            SyncLeftRightFromBreastFullness();
            SyncBaseFullness();
            return 0f;
        }
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
        float factor = totalWouldTake > remainingCap && totalWouldTake > 1E-6f ? remainingCap / totalWouldTake : 1f;
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
                totalDrained += take;
            }
        }
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
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
