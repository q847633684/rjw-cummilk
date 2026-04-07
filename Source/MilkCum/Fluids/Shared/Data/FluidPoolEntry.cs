using RimWorld;
using Verse;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>乳池条目：虚拟槽 <see cref="Site"/>（泌乳 SSOT）、容量、流速倍率、解剖左右标记、序号、代表部位（状态倍率解析）。</summary>
public readonly struct FluidPoolEntry
{
    /// <summary>虚拟槽；泌乳为左/右乳槽；精液为 <see cref="FluidSiteKind.TesticleLeft"/> / <see cref="FluidSiteKind.TesticleRight"/>（全身器官汇入两侧）。</summary>
    public FluidSiteKind Site { get; }
    public string Key { get; }
    public float Capacity { get; }
    /// <summary>该池的 RJW/基因等流速倍率；调用方 × basePerDay 得每日流速。泌乳储奶为 <c>稳定基键_L/_R</c> 时每格一条目，倍率同源 Hediff。</summary>
    public float FlowMultiplier { get; }
    /// <summary>仅解剖左乳为 true；解剖右与未标注为 false。右栏汇总仅计解剖右（与 IsAnatomicallyRightBreastPart 一致）。</summary>
    public bool IsLeft { get; }
    /// <summary>虚拟槽排序序号（左槽 0、右槽 1，仅一侧时 0）。</summary>
    public int PoolIndex { get; }
    /// <summary>挂乳房 Hediff 的身体部位；乳腺炎等与部位绑定的倍率用此解析。</summary>
    public BodyPartRecord SourcePart { get; }

    /// <summary>泌乳虚拟左/右槽：<see cref="Key"/> = <see cref="FluidSiteKind"/> 的枚举名（存档与网络一致）。</summary>
    public FluidPoolEntry(FluidSiteKind site, float capacity, float flowMultiplier, bool isLeft, int poolIndex, BodyPartRecord sourcePart = null)
        : this(site == FluidSiteKind.None ? "" : site.ToString(), site, capacity, flowMultiplier, isLeft, poolIndex, sourcePart)
    {
    }

    /// <summary>自定义池键（如每叶 <c>MakeStablePoolKey</c>）；<paramref name="site"/> 可为 <see cref="FluidSiteKind.None"/>。</summary>
    public FluidPoolEntry(string key, FluidSiteKind site, float capacity, float flowMultiplier, bool isLeft, int poolIndex, BodyPartRecord sourcePart = null)
    {
        Key = key ?? "";
        Site = site;
        Capacity = capacity;
        FlowMultiplier = flowMultiplier;
        IsLeft = isLeft;
        PoolIndex = poolIndex;
        SourcePart = sourcePart;
    }
}
