using System.Collections.Generic;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

/// <summary>
/// 乳房穿戴式挤奶：泌乳状态下按间隔从 <see cref="CompEquallyMilkable"/> 乳池扣量并 <see cref="CompEquallyMilkable.SpawnBottlesForDrainedAmount"/>，与手挤/机器共用同一套池与奶瓶逻辑。
/// </summary>
public class Hediff_BreastMilkingApparel : HediffWithComps
{
    private const int BreastMilkingPulseTicks = 2500;

    public override void Tick()
    {
        base.Tick();
        Pawn p = pawn;
        if (p == null || !p.Spawned || p.Dead) return;
        if (!p.IsHashIntervalTick(BreastMilkingPulseTicks)) return;
        if (!(p.IsColonist || p.IsPrisoner || p.IsSlave)) return;
        if (!p.health.hediffSet.HasHediff(HediffDefOf.Lactating)) return;

        CompEquallyMilkable comp = p.CompEquallyMilkable();
        if (comp == null || comp.Fullness < 1f) return;

        float take = Mathf.Floor(comp.Fullness);
        if (take < 1f) return;

        var drainedKeys = new List<string>();
        float drained = comp.DrainForConsume(take, drainedKeys);
        if (drained > 0f)
            comp.SpawnBottlesForDrainedAmount(drained, p, null, drainedKeys, skipMilkingMoodMemories: true);
    }
}
