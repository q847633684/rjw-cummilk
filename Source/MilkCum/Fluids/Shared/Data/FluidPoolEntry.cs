using RimWorld;
using Verse;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>乳池条目：稳定 key、容量、流速倍率、解剖左右标记、对序号、RJW 乳房部位。</summary>
public readonly struct FluidPoolEntry
{
    public string Key { get; }
    public float Capacity { get; }
    /// <summary>该池的 RJW/基因等流速倍率；调用方 × basePerDay 得每日流速。合并双侧虚拟池时为原单侧倍率的 2 倍以保持总进水量不变。</summary>
    public float FlowMultiplier { get; }
    /// <summary>仅解剖左乳为 true；解剖右与未标注为 false。双池进水时与对侧成对；右栏汇总仅计解剖右（与 IsAnatomicallyRightBreastPart 一致）。</summary>
    public bool IsLeft { get; }
    /// <summary>第几对乳房（0=第一对）；解剖左/右乳各一条 Hediff 时二者共享同一 PairIndex，用于 TickGrowth 成对处理。</summary>
    public int PairIndex { get; }
    /// <summary>挂乳房 Hediff 的身体部位；乳腺炎等与部位绑定的倍率用此解析。</summary>
    public BodyPartRecord SourcePart { get; }

    public FluidPoolEntry(string key, float capacity, float flowMultiplier, bool isLeft, int pairIndex, BodyPartRecord sourcePart = null)
    {
        Key = key ?? "";
        Capacity = capacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
        PairIndex = pairIndex;
        SourcePart = sourcePart;
    }
}
