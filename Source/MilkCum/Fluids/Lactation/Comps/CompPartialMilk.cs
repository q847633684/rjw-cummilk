using Verse;

namespace MilkCum.Fluids.Lactation.Comps;

/// <summary>未满一瓶的人奶：存储实际取量（0~1 池单位），食用时营养按此值计算。</summary>
public class CompProperties_PartialMilk : CompProperties
{
    public float fillAmount = 0.5f;

    public CompProperties_PartialMilk()
    {
        compClass = typeof(CompPartialMilk);
    }
}

/// <summary>未满一瓶的人奶 Comp：fillAmount 为已取池单位（&lt;1），用于营养计算与存档。</summary>
public class CompPartialMilk : ThingComp
{
    public float fillAmount = 0.5f;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref fillAmount, "fillAmount", 0.5f);
    }
}
