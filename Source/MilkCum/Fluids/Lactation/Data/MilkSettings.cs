using Verse;

namespace MilkCum.Fluids.Lactation.Data;

public class MilkSettings : IExposable
{
    public bool allowMilking = true;
    public bool allowMilkingSelf = true;
    /// <summary>允许自己食用产出的奶/精液制品。关则自己也不能吃自己的制品。默认关，由「允许自己」里的「吃奶」勾选。</summary>
    public bool allowSelfConsumeProducts = false;
    public bool allowBreastFeeding = true;
    public bool allowBreastFeedingAdult = false;
    public bool canBeFed = true;

    public void ExposeData()
    {
        Scribe_Values.Look(ref allowMilking, "allowMilking", true, false);
        Scribe_Values.Look(ref allowMilkingSelf, "allowMilkingSelf", true, false);
        Scribe_Values.Look(ref allowSelfConsumeProducts, "allowSelfConsumeProducts", false, false);
        Scribe_Values.Look(ref allowBreastFeeding, "allowBreastFeeding", true, false);
        Scribe_Values.Look(ref allowBreastFeedingAdult, "allowBreastFeedingAdult", false, false);
        Scribe_Values.Look(ref canBeFed, "canBeFed", true, false);
    }
    public MilkSettings Copy()
    {
        MilkSettings copy = new()
        {
            allowMilking = allowMilking,
            allowMilkingSelf = allowMilkingSelf,
            allowSelfConsumeProducts = allowSelfConsumeProducts,
            allowBreastFeeding = allowBreastFeeding,
            allowBreastFeedingAdult = allowBreastFeedingAdult,
            canBeFed = canBeFed
        };
        return copy;
    }
}
