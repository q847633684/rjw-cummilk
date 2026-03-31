using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Cum.Comps;

/// <summary>每条可射精生殖器独立虚拟精池：存档键 <c>Semen_{loadID}</c>；旧档 <c>TesticleLeft/Right</c> 首次 Tick 按比例摊入各条池。</summary>
public class CompVirtualSemenPool : ThingComp
{
    private const string LegacyKeyLeft = "TesticleLeft";
    private const string LegacyKeyRight = "TesticleRight";

    private Dictionary<string, float> fullnessByKey = new Dictionary<string, float>();

    public override void CompTickInterval(int delta)
    {
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return;
        if (parent is not Pawn pawn) return;
        float days = delta / (float)GenDate.TicksPerDay;
        Refill(pawn, days);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref fullnessByKey, "EM.FluidSitesSemen", LookMode.Value, LookMode.Value);
        fullnessByKey ??= new Dictionary<string, float>();
    }

    /// <summary>扣减虚拟池并返回实际可供射精的体量；<paramref name="registerForFluidRecords"/> 为口交记录补丁提供登记。</summary>
    public float ConsumeForEjaculation(ISexPartHediff part, float nominal, bool registerForFluidRecords)
    {
        if (parent is not Pawn pawn) return nominal;
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool || nominal <= 0f) return nominal;
        if (!RjwSemenPoolEconomy.IsPenisLikeSemenPart(part)) return nominal;

        List<FluidPoolEntry> entries = RjwSemenPoolEconomy.BuildSemenPoolEntries(pawn);
        if (entries.Count == 0) return nominal;

        MergeLegacyAggregateKeysIntoPerPartPools(entries);
        EnsureKeysInitialized(entries);
        var keys = new List<string>(2);
        RjwSemenPoolEconomy.AddVirtualSemenKeysForPart(part, keys);
        if (keys.Count == 0) return nominal;

        float avail = 0f;
        for (int i = 0; i < keys.Count; i++)
        {
            if (fullnessByKey.TryGetValue(keys[i], out float v)) avail += v;
        }

        float actual = Mathf.Min(nominal, avail);
        if (actual <= PoolModelConstants.Epsilon) return 0f;

        string k = keys[0];
        fullnessByKey.TryGetValue(k, out float cur);
        fullnessByKey[k] = Mathf.Max(0f, cur - actual);

        if (registerForFluidRecords)
            VirtualSemenRecordLedger.Push(pawn, part, actual);
        return actual;
    }

    private void MergeLegacyAggregateKeysIntoPerPartPools(List<FluidPoolEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;
        bool hasLegacy = fullnessByKey.ContainsKey(LegacyKeyLeft) || fullnessByKey.ContainsKey(LegacyKeyRight);
        if (!hasLegacy) return;

        float legacySum = 0f;
        if (fullnessByKey.TryGetValue(LegacyKeyLeft, out float lv)) legacySum += lv;
        if (fullnessByKey.TryGetValue(LegacyKeyRight, out float rv)) legacySum += rv;
        fullnessByKey.Remove(LegacyKeyLeft);
        fullnessByKey.Remove(LegacyKeyRight);
        if (legacySum <= PoolModelConstants.Epsilon) return;

        float sumCap = 0f;
        for (int i = 0; i < entries.Count; i++)
            sumCap += Mathf.Max(0f, entries[i].Capacity);

        if (sumCap < 0.001f)
        {
            float share = legacySum / entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                string k = entries[i].Key;
                if (string.IsNullOrEmpty(k)) continue;
                float cap = entries[i].Capacity;
                fullnessByKey[k] = Mathf.Min(cap, share);
            }
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                string k = entries[i].Key;
                if (string.IsNullOrEmpty(k)) continue;
                float cap = Mathf.Max(0f, entries[i].Capacity);
                float assign = legacySum * (cap / sumCap);
                fullnessByKey[k] = Mathf.Min(cap, assign);
            }
        }
    }

    private void EnsureKeysInitialized(List<FluidPoolEntry> entries)
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            string k = entries[i].Key;
            if (string.IsNullOrEmpty(k)) continue;
            if (!fullnessByKey.ContainsKey(k))
                fullnessByKey[k] = entries[i].Capacity;
        }
    }

    private void Refill(Pawn pawn, float deltaDays)
    {
        List<FluidPoolEntry> entries = RjwSemenPoolEconomy.BuildSemenPoolEntries(pawn);
        if (entries.Count == 0)
        {
            fullnessByKey.Clear();
            return;
        }

        MergeLegacyAggregateKeysIntoPerPartPools(entries);
        EnsureKeysInitialized(entries);
        float daysFull = Mathf.Max(0.01f, MilkCumSettings.Cum_SemenPoolDaysForFullRefill);
        float baseFrac = Mathf.Min(1f, deltaDays / daysFull);
        for (int i = 0; i < entries.Count; i++)
        {
            FluidPoolEntry e = entries[i];
            string k = e.Key;
            if (string.IsNullOrEmpty(k)) continue;
            fullnessByKey.TryGetValue(k, out float cur);
            float cap = e.Capacity;
            float deficit = Mathf.Max(0f, cap - cur);
            float add = deficit * Mathf.Min(1f, baseFrac * Mathf.Max(0.01f, e.FlowMultiplier));
            fullnessByKey[k] = Mathf.Min(cap, cur + add);
        }

        PruneOrphanKeys(entries);
    }

    private void PruneOrphanKeys(List<FluidPoolEntry> entries)
    {
        var valid = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
            if (!string.IsNullOrEmpty(entries[i].Key)) valid.Add(entries[i].Key);
        var toRemove = new List<string>();
        foreach (string k in fullnessByKey.Keys)
            if (!valid.Contains(k)) toRemove.Add(k);
        for (int i = 0; i < toRemove.Count; i++)
            fullnessByKey.Remove(toRemove[i]);
    }

    /// <summary>健康页等 UI：各虚拟精池当前量与容量（与 <see cref="RjwSemenPoolEconomy.BuildSemenPoolEntries"/> 对齐）。</summary>
    public List<(string Label, float Current, float Capacity)> GetSemenPoolDisplayRows(Pawn pawn)
    {
        var result = new List<(string, float, float)>();
        if (pawn == null || parent != pawn) return result;
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return result;
        List<FluidPoolEntry> entries = RjwSemenPoolEconomy.BuildSemenPoolEntries(pawn);
        if (entries.Count == 0) return result;
        MergeLegacyAggregateKeysIntoPerPartPools(entries);
        EnsureKeysInitialized(entries);
        for (int i = 0; i < entries.Count; i++)
        {
            FluidPoolEntry e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            fullnessByKey.TryGetValue(e.Key, out float cur);
            string label = e.SourcePart?.LabelCap ?? e.Key;
            result.Add((label, cur, e.Capacity));
        }

        return result;
    }
}
