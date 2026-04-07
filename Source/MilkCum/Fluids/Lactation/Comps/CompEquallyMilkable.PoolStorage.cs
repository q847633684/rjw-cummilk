using System.Collections.Generic;
using MilkCum.Core.Constants;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using MilkCum.RJW;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池存储与同步：缓存条目、按 key/左右侧汇总视图、选侧规则、清空/设置总水量。见 记忆库/docs/单池模型、ADR-003-选侧先左。</summary>
public partial class CompEquallyMilkable
{
    /// <summary>缓存乳池条目（与侧行同次构建）；脏或超过 <see cref="GetEntriesCacheMaxTicksForPawn"/> 时失效。</summary>
    private List<FluidPoolEntry> cachedEntries;
    /// <summary>与 <see cref="cachedEntries"/> 同次构建，避免 UI/联动再调 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/>。</summary>
    private List<RjwBreastPoolSideRow> cachedSideRows;
    private int cachedEntriesTick = -1;
    /// <summary>为 true 时下次 GetCachedEntries 强制重建；120-tick 比较乳房数量或 ClearPools 时设脏。</summary>
    private bool cachedEntriesDirty = true;
    /// <summary>上次重建缓存时的乳房 Hediff 数量，用于 120-tick 检测变化并设脏。</summary>
    private int lastBreastListCount = -1;
    /// <summary>与乳房条数配套的 <see cref="RjwBreastPoolEconomy.BuildBreastListPoolKeySignature"/>，条数不变但 Part/键路径变化时设脏。</summary>
    private string lastBreastPoolKeySignature;
    /// <summary>当前地图小人：池条目缓存 TTL（tick）。非当前地图/未载入地图用更长 TTL，与 LOD 一致。</summary>
    private const int EntriesCacheMaxTicksOnCurrentMap = 300;
    /// <summary>同对满度相等时先扣解剖左，仍平则优先解剖右，与 DrainForConsume / GetFirstDrainSideIndex 一致（ADR-003）。</summary>
    private const bool PreferLeftWhenEqual = true;

    /// <summary>该乳池侧「达基础满阈」连续累计的 tick（与 UpdateMilkPools 同步）。</summary>
    internal int GetTicksFullPoolForKey(string key) =>
        ticksFullPoolByKey != null && ticksFullPoolByKey.TryGetValue(key, out int t) ? t : 0;

    /// <summary>各池 key 满池计数中的最大值（满池信件、化脓 MTB 等）。</summary>
    internal int GetMaxTicksFullPoolAcrossSides()
    {
        if (ticksFullPoolByKey == null || ticksFullPoolByKey.Count == 0) return 0;
        int m = 0;
        foreach (int v in ticksFullPoolByKey.Values)
            if (v > m) m = v;
        return m;
    }

    /// <summary>每 60tick 进水结束后：按各乳池侧是否 ≥ 基础容量×满阈 更新/衰减计数并剔除无效 key。</summary>
    internal void UpdateTicksFullPoolForEntries(List<FluidPoolEntry> entries)
    {
        if (entries == null) return;
        ticksFullPoolByKey ??= new Dictionary<string, int>();
        var valid = new HashSet<string>();
        float th = PoolModelConstants.FullnessThresholdFactor;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            valid.Add(e.Key);
            float cur = GetFullnessForKey(e.Key);
            bool sideFull = cur >= e.Capacity * th;
            int t = GetTicksFullPoolForKey(e.Key);
            if (sideFull) t += 60;
            else t = Mathf.Max(0, t - 120);
            ticksFullPoolByKey[e.Key] = t;
        }
        if (ticksFullPoolByKey.Count == 0) return;
        var toRemove = new List<string>();
        foreach (var k in ticksFullPoolByKey.Keys)
            if (!valid.Contains(k)) toRemove.Add(k);
        for (int i = 0; i < toRemove.Count; i++)
            ticksFullPoolByKey.Remove(toRemove[i]);
    }

    /// <summary>按 key 取该乳当前水位，用于健康页悬停等；无该 key 时返回 0</summary>
    public float GetFullnessForKey(string key)
    {
        if (string.IsNullOrEmpty(key) || breastFullness == null) return 0f;
        return breastFullness.TryGetValue(key, out float v) ? v : 0f;
    }

    /// <summary>左/右汇总视图按虚拟槽 <see cref="FluidSiteKind"/>；<see cref="Fullness"/> 为各槽之和。</summary>
    private float GetSideFullnessSum(bool isLeftSide)
    {
        if (breastFullness == null || breastFullness.Count == 0) return 0f;
        var entries = GetCachedEntries();
        float sum = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            if (!breastFullness.TryGetValue(e.Key, out float v)) continue;
            if (isLeftSide)
            {
                if (e.Site == FluidSiteKind.BreastLeft) sum += v;
            }
            else if (e.Site == FluidSiteKind.BreastRight)
            {
                sum += v;
            }
        }
        return sum;
    }

    /// <summary>非当前地图殖民者：条目缓存稍长，减少切地图/多地图时重复算 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/>。</summary>
    private const int EntriesCacheMaxTicksOtherMap = 600;

    /// <summary>未载入地图（商队等）：与 <see cref="PoolModelConstants.LODIntervalNotOnMapTicks"/> 同量级，避免频繁重建。</summary>
    private const int EntriesCacheMaxTicksNotOnMap = 1800;

    private int GetEntriesCacheMaxTicksForPawn()
    {
        if (Pawn == null) return EntriesCacheMaxTicksOnCurrentMap;
        if (Pawn.MapHeld == null) return EntriesCacheMaxTicksNotOnMap;
        return Find.CurrentMap == Pawn.MapHeld ? EntriesCacheMaxTicksOnCurrentMap : EntriesCacheMaxTicksOtherMap;
    }

    /// <summary>
    /// 瘀积满池 tick 与乳池键对齐：无新键时清空；奶量键集合不变时剔除无效 tick 键；否则按新池容量比例重分总 tick（最大余数法，守恒整数）。
    /// </summary>
    private void MigrateTicksFullPoolForNewEntryKeys(List<FluidPoolEntry> newEntries, HashSet<string> newKeySet, HashSet<string> oldBreastKeySet)
    {
        ticksFullPoolByKey ??= new Dictionary<string, int>();
        if (newKeySet.Count == 0)
        {
            ticksFullPoolByKey.Clear();
            return;
        }

        if (oldBreastKeySet.SetEquals(newKeySet))
        {
            var toRemove = new List<string>();
            foreach (var k in ticksFullPoolByKey.Keys)
                if (!newKeySet.Contains(k))
                    toRemove.Add(k);
            for (int i = 0; i < toRemove.Count; i++)
                ticksFullPoolByKey.Remove(toRemove[i]);
            return;
        }

        int totalTicks = 0;
        foreach (var kv in ticksFullPoolByKey)
            totalTicks += kv.Value;
        ticksFullPoolByKey.Clear();
        if (totalTicks <= 0) return;

        var capByKey = new Dictionary<string, float>();
        if (newEntries != null)
        {
            for (int i = 0; i < newEntries.Count; i++)
            {
                var e = newEntries[i];
                if (string.IsNullOrEmpty(e.Key)) continue;
                capByKey.TryGetValue(e.Key, out float c);
                capByKey[e.Key] = c + Mathf.Max(0f, e.Capacity);
            }
        }

        float sumCap = 0f;
        foreach (var kv in capByKey)
            sumCap += kv.Value;

        if (sumCap < 0.001f)
        {
            int baseShare = totalTicks / newKeySet.Count;
            int rem = totalTicks % newKeySet.Count;
            int n = 0;
            foreach (string k in newKeySet)
            {
                ticksFullPoolByKey[k] = baseShare + (n < rem ? 1 : 0);
                n++;
            }
            return;
        }

        var keys = new List<string>(capByKey.Count);
        var frac = new List<float>(capByKey.Count);
        foreach (var kv in capByKey)
        {
            float w = kv.Value / sumCap;
            float x = totalTicks * w;
            int fl = Mathf.FloorToInt(x);
            keys.Add(kv.Key);
            frac.Add(x - fl);
            ticksFullPoolByKey[kv.Key] = fl;
        }

        int assigned = 0;
        foreach (var kv in ticksFullPoolByKey)
            assigned += kv.Value;
        int leftover = totalTicks - assigned;
        if (leftover <= 0) return;

        var order = new List<int>(keys.Count);
        for (int i = 0; i < keys.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            float fa = frac[a], fb = frac[b];
            if (fa > fb + 1e-6f) return -1;
            if (fb > fa + 1e-6f) return 1;
            return string.CompareOrdinal(keys[a], keys[b]);
        });

        for (int j = 0; j < leftover && j < order.Count; j++)
        {
            string k = keys[order[j]];
            ticksFullPoolByKey[k] = ticksFullPoolByKey[k] + 1;
        }
    }

    /// <summary>
    /// 乳池条目键与上次存档不一致时（切换拓扑、叶路径变化等）：按新条目 <see cref="FluidPoolEntry.Capacity"/> 比例重分总奶量，守恒总量；键集合相同则不动。
    /// </summary>
    private void MigrateBreastFullnessForNewEntryKeys(List<FluidPoolEntry> newEntries)
    {
        breastFullness ??= new Dictionary<string, float>();
        var newKeySet = new HashSet<string>();
        if (newEntries != null)
        {
            for (int i = 0; i < newEntries.Count; i++)
            {
                string k = newEntries[i].Key;
                if (!string.IsNullOrEmpty(k)) newKeySet.Add(k);
            }
        }

        var oldBreastKeySet = new HashSet<string>();
        foreach (var k in breastFullness.Keys)
            if (!string.IsNullOrEmpty(k)) oldBreastKeySet.Add(k);

        var milkSnap = breastFullness.Count > 0
            ? new Dictionary<string, float>(breastFullness)
            : new Dictionary<string, float>();
        Pawn?.LactatingHediffComp()?.SyncPoolKeyedStateToEntries(milkSnap, newEntries ?? new List<FluidPoolEntry>());

        var oldKeySet = new HashSet<string>(breastFullness.Keys);
        if (newKeySet.Count == 0)
        {
            if (oldKeySet.Count > 0)
                breastFullness.Clear();
            MigrateTicksFullPoolForNewEntryKeys(newEntries, newKeySet, oldBreastKeySet);
            SyncBaseFullness();
            return;
        }

        if (oldKeySet.SetEquals(newKeySet))
        {
            MigrateTicksFullPoolForNewEntryKeys(newEntries, newKeySet, oldBreastKeySet);
            SyncBaseFullness();
            return;
        }

        float totalMilk = 0f;
        foreach (var v in breastFullness.Values) totalMilk += v;
        breastFullness.Clear();

        if (totalMilk <= PoolModelConstants.Epsilon)
        {
            MigrateTicksFullPoolForNewEntryKeys(newEntries, newKeySet, oldBreastKeySet);
            SyncBaseFullness();
            return;
        }

        var capByKeyForMilk = new Dictionary<string, float>();
        for (int i = 0; i < newEntries.Count; i++)
        {
            var e = newEntries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            capByKeyForMilk.TryGetValue(e.Key, out float c);
            capByKeyForMilk[e.Key] = c + Mathf.Max(0f, e.Capacity);
        }

        float sumCap = 0f;
        foreach (var kv in capByKeyForMilk)
            sumCap += kv.Value;

        if (sumCap < 0.001f)
        {
            float share = totalMilk / newKeySet.Count;
            foreach (string k in newKeySet)
                breastFullness[k] = share;
        }
        else
        {
            foreach (var kv in capByKeyForMilk)
            {
                float w = kv.Value / sumCap;
                breastFullness[kv.Key] = totalMilk * w;
            }
        }

        float stretch = PoolModelConstants.StretchCapFactor;
        foreach (var kv in capByKeyForMilk)
        {
            float maxF = kv.Value * stretch;
            if (breastFullness.TryGetValue(kv.Key, out float v) && v > maxF)
                breastFullness[kv.Key] = maxF;
        }

        MigrateTicksFullPoolForNewEntryKeys(newEntries, newKeySet, oldBreastKeySet);
        SyncBaseFullness();
    }

    private List<FluidPoolEntry> GetCachedEntries()
    {
        int now = Find.TickManager.TicksGame;
        int ttl = GetEntriesCacheMaxTicksForPawn();
        if (cachedEntries != null && !cachedEntriesDirty && (now - cachedEntriesTick) <= ttl)
            return cachedEntries;
        if (Pawn != null)
        {
            cachedSideRows = RjwBreastPoolEconomy.GetBreastPoolSideRows(Pawn);
            cachedEntries = PawnMilkPoolExtensions.BuildCachedBreastPoolEntries(Pawn);
            BreastPoolTopologyDiagnostics.MaybeDevWarnAfterEntriesBuilt(Pawn, cachedEntries);
            MigrateBreastFullnessForNewEntryKeys(cachedEntries);
        }
        else
        {
            cachedSideRows = null;
            cachedEntries = new List<FluidPoolEntry>();
        }
        cachedEntriesTick = now;
        cachedEntriesDirty = false;
        lastBreastListCount = Pawn != null ? Pawn.GetBreastListOrEmpty().Count : 0;
        lastBreastPoolKeySignature = Pawn != null ? RjwBreastPoolEconomy.BuildBreastListPoolKeySignature(Pawn) : "";
        return cachedEntries;
    }

    /// <summary>立即标记 entries 缓存为脏，下次 GetCachedEntries 将重建。泌乳/乳房 hediff 增删时调用。</summary>
    internal void SetEntriesCacheDirty()
    {
        cachedEntriesDirty = true;
    }

    /// <summary>每 120 tick 在 CompTick 中调用：乳房数量或池键签名变化时设脏，下次 GetCachedEntries 将重建。</summary>
    internal void EnsureEntriesCacheDirtyIfBreastCountChanged()
    {
        if (Pawn == null) return;
        int cur = Pawn.GetBreastListOrEmpty().Count;
        string sig = RjwBreastPoolEconomy.BuildBreastListPoolKeySignature(Pawn);
        if (cachedEntries != null && (cur != lastBreastListCount || sig != lastBreastPoolKeySignature))
            cachedEntriesDirty = true;
    }

    /// <summary>缓存有效时返回已缓存的池条目列表（只读使用），避免 Hediff/UI 重复分配；无效时返回 null，调用方需 fallback 到 GetBreastPoolEntries。</summary>
    internal List<FluidPoolEntry> GetCachedEntriesIfValid()
    {
        if (cachedEntries == null || Find.TickManager == null) return null;
        if (cachedEntriesDirty) return null;
        int now = Find.TickManager.TicksGame;
        if ((now - cachedEntriesTick) > GetEntriesCacheMaxTicksForPawn())
            return null;
        return cachedEntries;
    }

    /// <summary>与 <see cref="GetCachedEntriesIfValid"/> 同生命周期；供 RJW 联动等需 <see cref="RjwBreastPoolSideRow.BreastHediff"/> 的路径，避免再算一遍侧行。</summary>
    internal List<RjwBreastPoolSideRow> GetCachedSideRowsIfValid()
    {
        if (cachedEntries == null || Find.TickManager == null) return null;
        if (cachedEntriesDirty) return null;
        int now = Find.TickManager.TicksGame;
        if ((now - cachedEntriesTick) > GetEntriesCacheMaxTicksForPawn())
            return null;
        return cachedSideRows;
    }

    /// <summary>当前乳池条目数（多叶时等于可泌乳乳房叶数；与 <see cref="FluidPoolEntry"/> 条数一致）。</summary>
    public int BreastSideCount => GetCachedEntries().Count;

    /// <summary>吸奶/单侧扣量：在所有池条目中选<strong>单 key 满度最大</strong>的一条。顺序规则与手挤排序共用 <see cref="CompareDrainEntryOrder"/>。</summary>
    private int GetFirstDrainSideIndex()
    {
        var entries = GetCachedEntries();
        if (entries.Count == 0) return 0;
        int bestIdx = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.IsNullOrEmpty(entries[i].Key)) continue;
            if (bestIdx < 0 || CompareDrainEntryOrder(i, bestIdx, entries) < 0)
                bestIdx = i;
        }

        return bestIdx >= 0 ? bestIdx : 0;
    }

    /// <summary>将总池量同步到基类 fullness，供可能读取基类字段的代码使用。</summary>
    private void SyncBaseFullness()
    {
        float total = 0f;
        if (breastFullness != null)
        {
            foreach (var v in breastFullness.Values) total += v;
        }
        fullness = Mathf.Clamp(total, 0f, maxFullness);
    }

    /// <summary>泌乳结束时清空乳池（由 HediffComp_EqualMilkingLactating 调用）</summary>
    public void ClearPools()
    {
        breastFullness?.Clear();
        ticksFullPoolByKey?.Clear();
        cachedEntries = null;
        cachedEntriesDirty = true;
        lastBreastListCount = -1;
        lastBreastPoolKeySignature = null;
        SyncBaseFullness();
    }

    /// <summary>向指定池 key 追加奶量（用于 RJW produceFluidOnOrgasm 高潮产液等）。</summary>
    public void AddMilkToKeys(IEnumerable<(string key, float addAmount, float cap)> perKey)
    {
        if (breastFullness == null || perKey == null) return;
        foreach (var (key, addAmount, cap) in perKey)
        {
            if (string.IsNullOrEmpty(key) || addAmount <= 0f) continue;
            float cur = GetFullnessForKey(key);
            breastFullness[key] = Mathf.Min(cap, cur + addAmount);
        }
        SyncBaseFullness();
    }

    /// <summary>设置总奶量（0～上限）。按 key 比例缩放到目标总水量，唯一写入口之一。</summary>
    public void SetFullness(float value, float? cap = null)
    {
        float target = Mathf.Clamp(value, 0f, cap ?? maxFullness);
        float total = 0f;
        if (breastFullness != null)
        {
            foreach (var v in breastFullness.Values) total += v;
        }
        if (total <= 0f) { SyncBaseFullness(); return; }
        float factor = target / total;
        var keys = new List<string>(breastFullness.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string k = keys[i];
            breastFullness[k] = Mathf.Max(0f, breastFullness[k] * factor);
        }
        SyncBaseFullness();
    }
}
