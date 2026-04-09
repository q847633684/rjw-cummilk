using RimWorld;
using Verse;

namespace MilkCum.Fluids.MilkingApparel;

/// <summary>穿戴时添加/脱下时移除指定 Hediff（替代 VEF ApparelHediffs）。</summary>
public class CompProperties_MilkingApparel : CompProperties
{
    public HediffDef hediffWhileWorn;

    public CompProperties_MilkingApparel()
    {
        compClass = typeof(Comp_MilkingApparel);
    }
}
