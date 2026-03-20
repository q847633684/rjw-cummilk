using System.Collections.Generic;
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
    /// <summary>缓存 GetBreastPoolEntries()，脏或超过 EntriesCacheMaxTicks 时失效，减少分配与 GC。</summary>
    private List<FluidPoolEntry> cachedEntries;
    private int cachedEntriesTick = -1;
    /// <summary>为 true 时下次 GetCachedEntries 强制重建；120-tick 比较乳房数量或 ClearPools 时设脏。</summary>
    private bool cachedEntriesDirty = true;
    /// <summary>上次重建缓存时的乳房 Hediff 数量，用于 120-tick 检测变化并设脏。</summary>
    private int lastBreastListCount = -1;
    /// <summary>缓存最长有效 tick 数，超时则重建；与脏标记一起使用，避免长期不刷新。</summary>
    private const int EntriesCacheMaxTicks = 300;
    /// <summary>按 PairIndex 分组的缓存，与 cachedEntries 同周期失效。</summary>
    private List<List<FluidPoolEntry>> cachedPairGroups;
    /// <summary>同对满度相等时先扣左侧，与 DrainForConsume / GetFirstDrainSideIndex 一致（ADR-003）。</summary>
    private const bool PreferLeftWhenEqual = true;

    /// <summary>该侧是否处于溢出状态（已触发溢出且当前高于基础容量），用于停泌乳与回缩判定。</summary>
    private bool IsOverflowState(string key, float cur, float baseCap)
        => overflowTriggeredByKey.TryGetValue(key, out bool ov) && ov && cur > baseCap;

    /// <summary>与 IsOverflowState 一致，供 UI 流速显示使用：处于回缩状态的侧实际不进水，显示流速时应将该侧计为 0。</summary>
    internal bool IsOverflowStateForDisplay(string key, float cur, float baseCap)
        => IsOverflowState(key, cur, baseCap);

    /// <summary>按 key 取该乳当前水位，用于健康页悬停等；无该 key 时返回 0</summary>
    public float GetFullnessForKey(string key)
    {
        if (string.IsNullOrEmpty(key) || breastFullness == null) return 0f;
        return breastFullness.TryGetValue(key, out float v) ? v : 0f;
    }

    /// <summary>按 entries 汇总左侧或右侧总水位（LeftFullness/RightFullness 用），便于多乳/不对称。</summary>
    private float GetLeftOrRightFullness(bool left)
    {
        if (breastFullness == null || breastFullness.Count == 0) return 0f;
        var entries = GetCachedEntries();
        float sum = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.IsLeft != left || string.IsNullOrEmpty(e.Key)) continue;
            if (breastFullness.TryGetValue(e.Key, out float v)) sum += v;
        }
        return sum;
    }

    private List<FluidPoolEntry> GetCachedEntries()
    {
        int now = Find.TickManager.TicksGame;
        if (cachedEntries != null && !cachedEntriesDirty && (now - cachedEntriesTick) <= EntriesCacheMaxTicks)
            return cachedEntries;
        cachedEntries = Pawn != null ? Pawn.GetBreastPoolEntries() : new List<FluidPoolEntry>();
        cachedEntriesTick = now;
        cachedEntriesDirty = false;
        lastBreastListCount = Pawn != null ? Pawn.GetBreastListOrEmpty().Count : 0;
        cachedPairGroups = null;
        return cachedEntries;
    }

    /// <summary>立即标记 entries 缓存为脏，下次 GetCachedEntries 将重建。泌乳/乳房 hediff 增删时调用。</summary>
    internal void SetEntriesCacheDirty()
    {
        cachedEntriesDirty = true;
    }

    /// <summary>每 120 tick 在 CompTick 中调用：乳房数量变化时设脏，下次 GetCachedEntries 将重建。</summary>
    internal void EnsureEntriesCacheDirtyIfBreastCountChanged()
    {
        if (Pawn == null) return;
        int cur = Pawn.GetBreastListOrEmpty().Count;
        if (cachedEntries != null && cur != lastBreastListCount)
            cachedEntriesDirty = true;
    }

    /// <summary>缓存有效时返回已缓存的池条目列表（只读使用），避免 Hediff/UI 重复分配；无效时返回 null，调用方需 fallback 到 GetBreastPoolEntries。</summary>
    internal List<FluidPoolEntry> GetCachedEntriesIfValid()
    {
        if (cachedEntries == null || Find.TickManager == null) return null;
        if (cachedEntriesDirty) return null;
        int now = Find.TickManager.TicksGame;
        if ((now - cachedEntriesTick) > EntriesCacheMaxTicks)
            return null;
        return cachedEntries;
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

    /// <summary>池侧数量（左+右等），用于机器挤奶并行扣量时算每侧速率。使用缓存，避免每 tick 分配。</summary>
    public int BreastSideCount => Mathf.Max(1, GetCachedEntries().Count);

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

    /// <summary>按 PairIndex 分组并按该对总满度降序排列，用于 Drain 选侧（最满的对先扣）。无 LINQ，减少 GC。</summary>
    private List<List<FluidPoolEntry>> BuildPairGroupsByFullnessDescending(List<FluidPoolEntry> entries)
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
        float SumFullness(List<FluidPoolEntry> list)
        {
            float s = 0f;
            for (int j = 0; j < list.Count; j++)
                s += GetFullnessForKey(list[j].Key);
            return s;
        }
        pairGroups.Sort((a, b) => SumFullness(b).CompareTo(SumFullness(a)));
        return pairGroups;
    }

    /// <summary>吸奶/单侧扣量时「第一个会被扣」的侧在 GetCachedEntries() 中的下标；与 DrainForConsume(..., singleSideOnly: true) 选侧一致。</summary>
    private int GetFirstDrainSideIndex()
    {
        var entries = GetCachedEntries();
        if (entries.Count == 0) return 0;
        var pairGroups = BuildPairGroupsByFullnessDescending(entries);
        string singleKey = null;
        for (int g = 0; g < pairGroups.Count; g++)
        {
            var list = pairGroups[g];
            if (list.Count == 2)
            {
                FluidPoolEntry leftE = list[0].IsLeft ? list[0] : list[1];
                FluidPoolEntry rightE = list[0].IsLeft ? list[1] : list[0];
                if (string.IsNullOrEmpty(leftE.Key) || string.IsNullOrEmpty(rightE.Key)) continue;
                float leftF = GetFullnessForKey(leftE.Key);
                float rightF = GetFullnessForKey(rightE.Key);
                bool drainLeftFirst = leftF > rightF || (Mathf.Approximately(leftF, rightF) && PreferLeftWhenEqual);
                singleKey = drainLeftFirst ? leftE.Key : rightE.Key;
                break;
            }
            if (list.Count >= 1 && !string.IsNullOrEmpty(list[0].Key))
            {
                singleKey = list[0].Key;
                break;
            }
        }
        if (string.IsNullOrEmpty(singleKey)) return 0;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Key == singleKey) return i;
        return 0;
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
        cachedEntries = null;
        cachedPairGroups = null;
        cachedEntriesDirty = true;
        lastBreastListCount = -1;
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
