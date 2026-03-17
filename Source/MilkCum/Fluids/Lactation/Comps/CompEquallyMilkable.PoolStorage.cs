using System.Collections.Generic;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>池存储与同步：缓存条目、按 key/左右汇总、选侧分组、清空/设置总水量。见 Docs/双池与PairIndex、ADR-003-选侧先左。</summary>
public partial class CompEquallyMilkable
{
    /// <summary>缓存 GetBreastPoolEntries()，每 60 tick 失效，减少每 tick 分配与 GC。</summary>
    private List<FluidPoolEntry> cachedEntries;
    private int cachedEntriesTick = -1;
    /// <summary>按 PairIndex 分组的缓存，与 cachedEntries 同周期失效，避免每 60 tick 重建 Dictionary+List。</summary>
    private List<List<FluidPoolEntry>> cachedPairGroups;
    private const int CacheInvalidateInterval = 60;
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

    private List<FluidPoolEntry> GetCachedEntries()
    {
        int now = Find.TickManager.TicksGame;
        if (cachedEntries != null && (now - cachedEntriesTick) <= CacheInvalidateInterval)
            return cachedEntries;
        cachedEntries = Pawn != null ? Pawn.GetBreastPoolEntries() : new List<FluidPoolEntry>();
        cachedEntriesTick = now;
        cachedPairGroups = null;
        return cachedEntries;
    }

    /// <summary>缓存有效时返回已缓存的池条目列表（只读使用），避免 Hediff/UI 重复分配；无效时返回 null，调用方需 fallback 到 GetBreastPoolEntries。</summary>
    internal List<FluidPoolEntry> GetCachedEntriesIfValid()
    {
        if (cachedEntries == null || Find.TickManager == null) return null;
        int now = Find.TickManager.TicksGame;
        if ((now - cachedEntriesTick) <= CacheInvalidateInterval)
            return cachedEntries;
        return null;
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

    /// <summary>将双池总和同步到基类 fullness，供可能读取基类字段的代码使用</summary>
    private void SyncBaseFullness()
    {
        fullness = Mathf.Clamp(leftFullness + rightFullness, 0f, maxFullness);
    }

    /// <summary>从 per-breast 字典汇总到 leftFullness / rightFullness（按 GetBreastPoolEntries 的 IsLeft）</summary>
    private void SyncLeftRightFromBreastFullness()
    {
        if (Pawn == null || breastFullness == null) return;
        var entries = GetCachedEntries();
        float left = 0f, right = 0f;
        foreach (var e in entries)
        {
            if (breastFullness.TryGetValue(e.Key, out float v))
            {
                if (e.IsLeft) left += v; else right += v;
            }
        }
        leftFullness = left;
        rightFullness = right;
    }

    /// <summary>泌乳结束时清空双池（由 HediffComp_EqualMilkingLactating 调用）</summary>
    public void ClearPools()
    {
        leftFullness = 0f;
        rightFullness = 0f;
        breastFullness?.Clear();
        cachedEntries = null;
        cachedPairGroups = null;
        SyncBaseFullness();
    }

    /// <summary>向指定池 key 追加奶量（用于 RJW produceFluidOnOrgasm 高潮产液等），追加后同步左右汇总。</summary>
    public void AddMilkToKeys(IEnumerable<(string key, float addAmount, float cap)> perKey)
    {
        if (breastFullness == null || perKey == null) return;
        foreach (var (key, addAmount, cap) in perKey)
        {
            if (string.IsNullOrEmpty(key) || addAmount <= 0f) continue;
            float cur = GetFullnessForKey(key);
            breastFullness[key] = Mathf.Min(cap, cur + addAmount);
        }
        SyncLeftRightFromBreastFullness();
        SyncBaseFullness();
    }

    /// <summary>
    /// 设置总奶量（0～上限）。从各乳池按比例缩放到目标总水量。
    /// </summary>
    /// <param name="value">目标总水量</param>
    /// <param name="cap">可选；不传时用 maxFullness 为上限；传时用于 clamp（如关压力因子时用撑大总容量）</param>
    public void SetFullness(float value, float? cap = null)
    {
        float target = Mathf.Clamp(value, 0f, cap ?? maxFullness);
        float total = leftFullness + rightFullness;
        if (total <= 0f) { SyncBaseFullness(); return; }
        float factor = target / total;
        if (breastFullness != null)
        {
            var keys = new List<string>(breastFullness.Keys);
            foreach (var k in keys)
                breastFullness[k] = Mathf.Max(0f, breastFullness[k] * factor);
        }
        leftFullness *= factor;
        rightFullness *= factor;
        SyncBaseFullness();
    }
}
