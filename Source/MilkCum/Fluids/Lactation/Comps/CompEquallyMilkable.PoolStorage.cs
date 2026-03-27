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

/// <summary>池存储与同步：缓存条目、按 key/左右汇总、选侧分组、清空/设置总水量。见 Docs/双池与PairIndex、ADR-003-选侧先左。</summary>
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
    /// <summary>按 PairIndex 分组的缓存，与 cachedEntries 同周期失效。</summary>
    private List<List<FluidPoolEntry>> cachedPairGroups;
    /// <summary>同对满度相等时先扣解剖左，仍平则优先解剖右，与 DrainForConsume / GetFirstDrainSideIndex 一致（ADR-003）。</summary>
    private const bool PreferLeftWhenEqual = true;

    /// <summary>该侧「达基础满阈」连续累计的 tick（与 UpdateMilkPools 同步）。</summary>
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

    /// <summary>每 60tick 进水结束后：按各侧是否 ≥ 基础容量×满阈 更新/衰减计数并剔除无效 key。</summary>
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

    /// <summary>
    /// 左：仅 <see cref="FluidPoolEntry.IsLeft"/>（解剖左）。右：仅解剖右（<see cref="RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart"/>）。
    /// 未标注侧不计入左/右栏；<see cref="Fullness"/> 仍为各 key 之和。
    /// </summary>
    private float GetLeftOrRightFullness(bool left)
    {
        if (breastFullness == null || breastFullness.Count == 0) return 0f;
        var entries = GetCachedEntries();
        float sum = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.Key)) continue;
            if (!breastFullness.TryGetValue(e.Key, out float v)) continue;
            if (left)
            {
                if (e.IsLeft) sum += v;
            }
            else
            {
                if (RjwBreastPoolEconomy.IsAnatomicallyRightBreastPart(e.SourcePart)) sum += v;
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

    private List<FluidPoolEntry> GetCachedEntries()
    {
        int now = Find.TickManager.TicksGame;
        int ttl = GetEntriesCacheMaxTicksForPawn();
        if (cachedEntries != null && !cachedEntriesDirty && (now - cachedEntriesTick) <= ttl)
            return cachedEntries;
        if (Pawn != null)
        {
            cachedSideRows = RjwBreastPoolEconomy.GetBreastPoolSideRows(Pawn);
            cachedEntries = PawnMilkPoolExtensions.BuildBreastPoolEntriesFromSideRows(Pawn, cachedSideRows);
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
        cachedPairGroups = null;
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

    /// <summary>按 PairIndex 分组结果，与 GetCachedEntries() 同周期缓存，用于 UpdateMilkPools。</summary>
    private List<List<FluidPoolEntry>> GetCachedPairGroups()
    {
        var entries = GetCachedEntries();
        if (cachedPairGroups != null)
            return cachedPairGroups;
        cachedPairGroups = BuildPairGroupsByPairIndex(entries);
        return cachedPairGroups;
    }

    /// <summary>乳池行数（每条可泌乳叶一行），与 <see cref="RjwBreastPoolEconomy.GetBreastPoolSideRows"/> 一致；严格一对为 2，单侧/多乳为 1 或更多。用于机器挤奶并行扣量时算每侧速率。</summary>
    public int BreastSideCount => GetCachedEntries().Count;

    /// <summary>按 PairIndex 分组并按 PairIndex 顺序排列，用于 UpdateMilkPools。无 LINQ，减少 GC。</summary>
    private static List<List<FluidPoolEntry>> BuildPairGroupsByPairIndex(List<FluidPoolEntry> entries)
    {
        var pairIndexToGroup = new Dictionary<int, int>();
        var pairGroups = new List<List<FluidPoolEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!pairIndexToGroup.TryGetValue(e.PairIndex, out int idx))
            {
                idx = pairGroups.Count;
                pairIndexToGroup[e.PairIndex] = idx;
                pairGroups.Add(new List<FluidPoolEntry>());
            }
            pairGroups[idx].Add(e);
        }
        pairGroups.Sort((a, b) => (a.Count > 0 ? a[0].PairIndex : 0).CompareTo(b.Count > 0 ? b[0].PairIndex : 0));
        return pairGroups;
    }

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

    /// <summary>泌乳结束时清空双池（由 HediffComp_EqualMilkingLactating 调用）</summary>
    public void ClearPools()
    {
        breastFullness?.Clear();
        ticksFullPoolByKey?.Clear();
        cachedEntries = null;
        cachedPairGroups = null;
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
