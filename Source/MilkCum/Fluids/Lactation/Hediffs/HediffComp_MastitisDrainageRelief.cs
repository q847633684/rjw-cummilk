using MilkCum.Core.Constants;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Lactation.Hediffs;

/// <summary>
/// 医学贴近：排空乳房（池满度低于阈值）时，乳腺炎 severity 额外衰减。
/// 对应临床「排空是治疗核心」；与 HediffCompProperties_SeverityPerDay 叠加。见 Docs/泌乳系统-全部说明。
/// </summary>
public class HediffComp_MastitisDrainageRelief : HediffComp
{
    public HediffCompProperties_MastitisDrainageRelief Props => (HediffCompProperties_MastitisDrainageRelief)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (parent?.pawn == null || !parent.pawn.Spawned) return;
        if (parent.pawn.IsHashIntervalTick(60) == false) return;

        var milkComp = parent.pawn.CompEquallyMilkable();
        if (milkComp == null) return;

        float maxF = Mathf.Max(0.001f, milkComp.maxFullness);
        float ratio = milkComp.Fullness / maxF;
        float threshold = Props.fullnessThreshold >= 0f ? Props.fullnessThreshold : PoolModelConstants.MastitisDrainageReliefFullnessThreshold;
        if (ratio >= threshold) return;

        float extraPerDay = Props.drainageReliefSeverityPerDay >= 0f ? Props.drainageReliefSeverityPerDay : PoolModelConstants.MastitisDrainageReliefSeverityPerDay;
        float delta = extraPerDay * (60f / 60000f);
        severityAdjustment -= delta;
    }
}

public class HediffCompProperties_MastitisDrainageRelief : HediffCompProperties
{
    /// <summary>池满度低于此比例时视为排空，触发额外缓解。&lt;0 表示使用 PoolModelConstants.MastitisDrainageReliefFullnessThreshold。</summary>
    public float fullnessThreshold = -1f;
    /// <summary>排空状态下每日额外 severity 衰减。&lt;0 表示使用 PoolModelConstants.MastitisDrainageReliefSeverityPerDay。</summary>
    public float drainageReliefSeverityPerDay = -1f;

    public HediffCompProperties_MastitisDrainageRelief() => compClass = typeof(HediffComp_MastitisDrainageRelief);
}
