using Verse;

namespace MilkCum.Fluids.Lactation.Data;

public class MilkSettings : IExposable
{
	/// <summary>
	/// 允许挤奶。默认开。
	/// </summary>
    public bool allowMilking = true;
	/// <summary>
	/// 允许自己挤奶。默认开。
	/// </summary>
    public bool allowMilkingSelf = true;
    /// <summary>允许自己食用产出的奶/精液制品。默认开。</summary>
    public bool allowSelfConsumeProducts = true;
	/// <summary>
	/// 允许自己哺乳。默认关。
	/// 哺乳：殖民者哺乳，动物哺乳，机械哺乳
	/// </summary>
    public bool allowBreastFeeding = true;
	/// <summary>
	/// 允许成年殖民者哺乳。默认关。
	/// </summary>
    public bool allowBreastFeedingAdult = false;
	/// <summary>
	/// 允许被哺乳。默认开。
	/// </summary>
    public bool canBeFed = true;

    public void ExposeData()
    {
        Scribe_Values.Look(ref allowMilking, "allowMilking", true, false);
        Scribe_Values.Look(ref allowMilkingSelf, "allowMilkingSelf", true, false);
        Scribe_Values.Look(ref allowSelfConsumeProducts, "allowSelfConsumeProducts", true, false);
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
