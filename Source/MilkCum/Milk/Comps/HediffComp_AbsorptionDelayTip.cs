using MilkCum.Milk.World;
using Verse;
using UnityEngine;

namespace MilkCum.Milk.Comps;

/// <summary>吸收延迟 hediff 悬停时显示距最早一批泌乳生效的剩余时间。见 Docs/泌乳系统逻辑图。</summary>
public class HediffComp_AbsorptionDelayTip : HediffComp
{
    public override string CompTipStringExtra
    {
        get
        {
            if (parent?.pawn == null) return null;
            int remaining = WorldComponent_EqualMilkingAbsorptionDelay.GetRemainingTicksForPawn(parent.pawn);
            if (remaining <= 0) return null;
            string timeStr = TicksToTimeString(remaining);
            return "EM.AbsorptionDelayRemaining".Translate(timeStr).Resolve();
        }
    }

    /// <summary>游戏内 2500 tick = 1 小时。</summary>
    private static string TicksToTimeString(int ticks)
    {
        const int ticksPerHour = 2500;
        if (ticks < ticksPerHour)
        {
            int minutes = Mathf.Max(1, (int)(ticks * 60f / ticksPerHour));
            return "EM.AbsorptionDelayMinutes".Translate(minutes).Resolve();
        }
        float hours = ticks / (float)ticksPerHour;
        return "EM.AbsorptionDelayHours".Translate(hours.ToString("0.#")).Resolve();
    }
}

public class HediffCompProperties_AbsorptionDelayTip : HediffCompProperties
{
    public HediffCompProperties_AbsorptionDelayTip() => compClass = typeof(HediffComp_AbsorptionDelayTip);
}
