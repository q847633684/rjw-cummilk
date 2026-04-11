using Verse;

namespace MilkCum.Fluids.Shared.Comps;

/// <summary>奶瓶品质（0~1）：由产奶时泌乳者状态决定，可用于描述/交易等。有乳腺炎时降低，L 高时提高。</summary>
public class CompProperties_MilkQuality : CompProperties
{
    public CompProperties_MilkQuality()
    {
        compClass = typeof(CompMilkQuality);
    }
}

/// <summary>奶瓶品质 Comp：quality 0~1，产奶时由 SpawnBottles 根据泌乳者 L/乳腺炎等写入。</summary>
public class CompMilkQuality : ThingComp
{
    public float quality = 0.7f;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref quality, "quality", 0.7f);
    }
}
