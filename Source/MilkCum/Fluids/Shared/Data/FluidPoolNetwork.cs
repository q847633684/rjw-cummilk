using System.Collections.Generic;
using System.Linq;
using MilkCum.Core.Constants;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>N 池器官网络：将多个独立池抽象为节点图，供进水/导管阻力/扣量统一使用。</summary>
public sealed class FluidPoolNetwork
{
    public readonly struct Node
    {
        public readonly string Key;
        public readonly int PoolIndex;
        public readonly float BaseCapacity;
        public readonly float StretchCapacity;
        public readonly float Fullness;
        public readonly bool IsLeft;
        public readonly BodyPartRecord SourcePart;

        public Node(string key, int poolIndex, float baseCapacity, float stretchCapacity, float fullness, bool isLeft, BodyPartRecord sourcePart)
        {
            Key = key ?? "";
            PoolIndex = poolIndex;
            BaseCapacity = Mathf.Max(0.001f, baseCapacity);
            StretchCapacity = Mathf.Max(BaseCapacity, stretchCapacity);
            Fullness = Mathf.Max(0f, fullness);
            IsLeft = isLeft;
            SourcePart = sourcePart;
        }
    }

    private readonly List<Node> nodes = new();
    private readonly Dictionary<string, int> keyToIndex = new();
    private readonly Dictionary<int, List<int>> adjacency = new();

    public IReadOnlyList<Node> Nodes => nodes;
    public int Count => nodes.Count;

    public static FluidPoolNetwork Build(IReadOnlyList<FluidPoolEntry> entries, IReadOnlyDictionary<string, float> fullnessByKey)
    {
        var g = new FluidPoolNetwork();
        if (entries == null || entries.Count == 0) return g;
        var sorted = entries
            .Where(e => !string.IsNullOrEmpty(e.Key))
            .OrderBy(e => e.PoolIndex)
            .ThenBy(e => e.Key)
            .ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            var e = sorted[i];
            float f = 0f;
            if (fullnessByKey != null && fullnessByKey.TryGetValue(e.Key, out float v)) f = v;
            var n = new Node(e.Key, e.PoolIndex, e.Capacity, PoolModelConstants.CapacityStretchCap(e.Capacity), f, e.IsLeft, e.SourcePart);
            g.keyToIndex[e.Key] = g.nodes.Count;
            g.nodes.Add(n);
        }

        // 简单链式邻接：按 PoolIndex 相邻池连边，支持 N 池扩展与“就近导管”近似。
        for (int i = 0; i < g.nodes.Count; i++)
        {
            g.adjacency[i] = new List<int>(2);
            if (i > 0) g.adjacency[i].Add(i - 1);
            if (i + 1 < g.nodes.Count) g.adjacency[i].Add(i + 1);
        }
        return g;
    }

    public bool TryGetNode(string key, out Node node)
    {
        node = default;
        if (string.IsNullOrEmpty(key)) return false;
        if (!keyToIndex.TryGetValue(key, out int idx)) return false;
        if (idx < 0 || idx >= nodes.Count) return false;
        node = nodes[idx];
        return true;
    }

    public float GetPressureRatio01(string key)
    {
        if (!TryGetNode(key, out var n)) return 0f;
        return Mathf.Clamp01(n.Fullness / Mathf.Max(0.001f, n.StretchCapacity));
    }

    public float GetOutletHopFactor(string key, float hopPenaltyPerEdge = 0.15f)
    {
        if (!keyToIndex.TryGetValue(key, out int idx) || nodes.Count <= 1) return 1f;
        int minHopsToEdge = Mathf.Min(idx, (nodes.Count - 1) - idx);
        // 边缘更容易排出；中心池导管路径更长。
        float p = Mathf.Max(0f, hopPenaltyPerEdge);
        return 1f / (1f + p * minHopsToEdge);
    }
}
