using Verse;

namespace EqualMilking.Data;

public class MilkSettings : IExposable
{
    public bool allowMilking = true;
    public bool allowMilkingSelf = true;
    public bool allowBreastFeeding = true;
    public bool allowBreastFeedingAdult = false;
    public bool canBeFed = true;

    public void ExposeData()
    {
        Scribe_Values.Look(ref allowMilking, "allowMilking", true, false);
        Scribe_Values.Look(ref allowMilkingSelf, "allowMilkingSelf", true, false);
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
            allowBreastFeeding = allowBreastFeeding,
            allowBreastFeedingAdult = allowBreastFeedingAdult,
            canBeFed = canBeFed
        };
        return copy;
    }
}