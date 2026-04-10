using System;
using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Cum.Common;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration.RjwBallsOvaries;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;
using System.Linq;

namespace MilkCum.Fluids.Cum.Comps;

/// <summary>左/右虚拟睾丸精池：每个 <see cref="Pawn"/> 一份 <c>fullnessByKey</c>；该小人身上可射精器官汇入两侧；存档键 TesticleLeft/TesticleRight。</summary>
public class CompVirtualSemenPool : ThingComp
{
    private const string ObsoletePerPartSemenPrefix = "Semen_";

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

        PreparePoolState(entries);
        var keys = new List<string>(2);
        RjwSemenPoolEconomy.AddVirtualSemenKeysForPart(part, keys);
        if (keys.Count == 0) return nominal;

        float avail = 0f;
        for (int i = 0; i < keys.Count; i++)
        {
            if (fullnessByKey.TryGetValue(keys[i], out float v)) avail += v;
        }

        float actual = Mathf.Min(nominal, avail);
        actual *= RjwBallsOvariesIntegration.GetVirtualSemenDrawFactor(pawn);
        if (actual <= PoolModelConstants.Epsilon) return 0f;

        if (keys.Count == 1)
        {
            string k = keys[0];
            fullnessByKey.TryGetValue(k, out float cur);
            fullnessByKey[k] = Mathf.Max(0f, cur - actual);
        }
        else
        {
            string kL = RjwSemenPoolEconomy.TesticleLeftPoolKey;
            string kR = RjwSemenPoolEconomy.TesticleRightPoolKey;
            fullnessByKey.TryGetValue(kL, out float l);
            fullnessByKey.TryGetValue(kR, out float r);
            float tot = l + r;
            if (tot <= PoolModelConstants.Epsilon) return 0f;
            float fromL = actual * (l / tot);
            float fromR = actual - fromL;
            fullnessByKey[kL] = Mathf.Max(0f, l - fromL);
            fullnessByKey[kR] = Mathf.Max(0f, r - fromR);
        }

        if (registerForFluidRecords)
            VirtualSemenRecordLedger.Push(pawn, part, actual);
        return actual;
    }

    /// <summary>较早版本每条阴茎 <c>Semen_{loadID}</c> 键：合并入当前左/右槽（按容量比例），再删除旧键。</summary>
    private void MergeObsoletePerPartSemenKeysIntoLateral(List<FluidPoolEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;
        float orphanSum = 0f;
        var remove = new List<string>();
        foreach (KeyValuePair<string, float> kv in fullnessByKey)
        {
            if (kv.Key == null || !kv.Key.StartsWith(ObsoletePerPartSemenPrefix, StringComparison.Ordinal)) continue;
            orphanSum += kv.Value;
            remove.Add(kv.Key);
        }

        for (int i = 0; i < remove.Count; i++)
            fullnessByKey.Remove(remove[i]);
        if (orphanSum <= PoolModelConstants.Epsilon) return;

        float sumCap = 0f;
        for (int i = 0; i < entries.Count; i++)
            sumCap += Mathf.Max(0f, entries[i].Capacity);
        if (sumCap < PoolModelConstants.FloatDustEpsilon)
        {
            float share = orphanSum / entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                string k = entries[i].Key;
                if (string.IsNullOrEmpty(k)) continue;
                float cap = entries[i].Capacity;
                fullnessByKey.TryGetValue(k, out float cur);
                fullnessByKey[k] = Mathf.Min(cap, cur + share);
            }

            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            string k = entries[i].Key;
            if (string.IsNullOrEmpty(k)) continue;
            float cap = Mathf.Max(0f, entries[i].Capacity);
            float add = orphanSum * (cap / sumCap);
            fullnessByKey.TryGetValue(k, out float cur);
            fullnessByKey[k] = Mathf.Min(cap, cur + add);
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

    /// <summary>
    /// 拓扑变化（键集合变化）时，按新键容量比例重分旧总量；键集合不变时保持原键值。
    /// 该迁移用于精池左右槽切换/缺失恢复等场景，尽量保持总量连续。
    /// </summary>
    private void MigrateFullnessForNewTopology(List<FluidPoolEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            fullnessByKey.Clear();
            return;
        }

        var capByKey = new Dictionary<string, float>();
        for (int i = 0; i < entries.Count; i++)
        {
            string k = entries[i].Key;
            if (string.IsNullOrEmpty(k)) continue;
            capByKey.TryGetValue(k, out float c);
            capByKey[k] = c + Mathf.Max(0f, entries[i].Capacity);
        }

        var newKeys = new HashSet<string>(capByKey.Keys);
        if (newKeys.Count == 0)
        {
            fullnessByKey.Clear();
            return;
        }

        var oldKeys = new HashSet<string>(fullnessByKey.Keys.Where(k => !string.IsNullOrEmpty(k)));
        if (oldKeys.SetEquals(newKeys))
            return;

        float total = 0f;
        foreach (float v in fullnessByKey.Values)
            total += Mathf.Max(0f, v);
        fullnessByKey.Clear();
        if (total <= PoolModelConstants.Epsilon)
            return;

        float sumCap = 0f;
        foreach (float c in capByKey.Values)
            sumCap += Mathf.Max(0f, c);
        if (sumCap < PoolModelConstants.FloatDustEpsilon)
        {
            float share = total / newKeys.Count;
            foreach (string k in newKeys)
                fullnessByKey[k] = share;
            return;
        }

        foreach (var kv in capByKey)
            fullnessByKey[kv.Key] = total * (Mathf.Max(0f, kv.Value) / sumCap);
    }

    /// <summary>按当前条目容量即刻钳制精池值，避免降容后一段时间仍保留超上限值。</summary>
    private void ClampToEntryCaps(List<FluidPoolEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            fullnessByKey.Clear();
            return;
        }

        var capByKey = new Dictionary<string, float>();
        for (int i = 0; i < entries.Count; i++)
        {
            string k = entries[i].Key;
            if (string.IsNullOrEmpty(k)) continue;
            capByKey.TryGetValue(k, out float c);
            capByKey[k] = c + Mathf.Max(0f, entries[i].Capacity);
        }

        var toRemove = new List<string>();
        foreach (string k in fullnessByKey.Keys)
        {
            if (!capByKey.ContainsKey(k))
                toRemove.Add(k);
        }
        for (int i = 0; i < toRemove.Count; i++)
            fullnessByKey.Remove(toRemove[i]);

        foreach (var kv in capByKey)
        {
            fullnessByKey.TryGetValue(kv.Key, out float cur);
            fullnessByKey[kv.Key] = Mathf.Clamp(cur, 0f, kv.Value);
        }
    }

    private void PreparePoolState(List<FluidPoolEntry> entries)
    {
        MergeObsoletePerPartSemenKeysIntoLateral(entries);
        MigrateFullnessForNewTopology(entries);
        EnsureKeysInitialized(entries);
        ClampToEntryCaps(entries);
    }

    private void Refill(Pawn pawn, float deltaDays)
    {
        List<FluidPoolEntry> entries = RjwSemenPoolEconomy.BuildSemenPoolEntries(pawn);
        if (entries.Count == 0)
        {
            fullnessByKey.Clear();
            return;
        }

        PreparePoolState(entries);
        float daysFull = Mathf.Max(0.01f, MilkCumSettings.Cum_SemenPoolDaysForFullRefill);
        float baseFrac = Mathf.Min(1f, deltaDays / daysFull);
        float refillBallz = RjwBallsOvariesIntegration.GetVirtualSemenRefillMultiplier(pawn);
        for (int i = 0; i < entries.Count; i++)
        {
            FluidPoolEntry e = entries[i];
            string k = e.Key;
            if (string.IsNullOrEmpty(k)) continue;
            fullnessByKey.TryGetValue(k, out float cur);
            float cap = e.Capacity;
            float deficit = Mathf.Max(0f, cap - cur);
            float add = deficit * Mathf.Min(1f, baseFrac * Mathf.Max(0.01f, e.FlowMultiplier) * refillBallz);
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

    /// <summary>健康页：左/右虚拟睾丸槽当前量与容量。</summary>
    public List<(FluidSiteKind Site, float Current, float Capacity)> GetSemenPoolDisplayRows(Pawn pawn)
    {
        var result = new List<(FluidSiteKind, float, float)>();
        if (pawn == null || parent != pawn) return result;
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return result;
        List<FluidPoolEntry> entries = RjwSemenPoolEconomy.BuildSemenPoolEntries(pawn);
        if (entries.Count == 0) return result;
        PreparePoolState(entries);
        for (int i = 0; i < entries.Count; i++)
        {
            FluidPoolEntry e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            fullnessByKey.TryGetValue(e.Key, out float cur);
            result.Add((e.Site, cur, e.Capacity));
        }

        return result;
    }
}
