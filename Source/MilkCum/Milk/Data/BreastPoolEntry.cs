namespace MilkCum.Milk.Data;

/// <summary>单乳池条目：稳定 key（Part.def.defName 或 defName_index）、容量、流速倍率（× basePerDay 得每日流速）、是否左侧（用于左/右汇总）、对序号（同一对左右共享同一 PairIndex，用于按对撑大与挤奶顺序）。</summary>
public readonly struct BreastPoolEntry
{
    public string Key { get; }
    public float Capacity { get; }
    /// <summary>该乳的流速倍率，与 GetMilkFlowMultipliersFromRJW 同源；调用方 × basePerDay 得每日流速。</summary>
    public float FlowMultiplier { get; }
    public bool IsLeft { get; }
    /// <summary>第几对乳房（0=第一对）；同一对的左乳与右乳共享同一值，用于按对撑大与挤奶从第一对开始选侧。</summary>
    public int PairIndex { get; }

    public BreastPoolEntry(string key, float capacity, float flowMultiplier, bool isLeft, int pairIndex = 0)
    {
        Key = key ?? "";
        Capacity = capacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
        PairIndex = pairIndex;
    }
}
