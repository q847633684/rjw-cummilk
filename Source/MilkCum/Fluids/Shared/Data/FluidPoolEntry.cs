namespace MilkCum.Fluids.Shared.Data;

/// <summary>单侧池条目：稳定 key、容量、流速倍率、是否左侧、对序号（乳汁/精液共享）。</summary>
public readonly struct FluidPoolEntry
{
    public string Key { get; }
    public float Capacity { get; }
    /// <summary>该乳的流速倍率，与 GetMilkFlowMultipliersFromRJW 同源；调用方 × basePerDay 得每日流速。</summary>
    public float FlowMultiplier { get; }
    public bool IsLeft { get; }
    /// <summary>第几对乳房（0=第一对）；同一对的左乳与右乳共享同一值，用于按对撑大与挤奶从第一对开始选侧。</summary>
    public int PairIndex { get; }
    /// <summary>RJW PartSizeConfigDef.density：仅用于「奶量增加」时放大进池量（泌乳进水、高潮产液等），不参与容量/流速倍率。人类 1.0。</summary>
    public float Density { get; }

    public FluidPoolEntry(string key, float capacity, float flowMultiplier, bool isLeft, int pairIndex = 0, float density = 1f)
    {
        Key = key ?? "";
        Capacity = capacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
        PairIndex = pairIndex;
        Density = density;
    }
}
