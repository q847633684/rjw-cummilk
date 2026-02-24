using Verse;

namespace EqualMilking.Data;
public abstract class Breastfeed : IExposable
{
    public bool AllowBreastfeeding;
    public bool BreastfeedHumanlike;
    public bool BreastfeedAnimal;
    public bool BreastfeedMechanoid;

    public virtual void ExposeData()
    {
        Scribe_Values.Look<bool>(ref this.AllowBreastfeeding, "AllowBreastfeeding", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedHumanlike, "BreastfeedHumanlike", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedAnimal, "BreastfeedAnimal", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedMechanoid, "BreastfeedMechanoid", false, false);
    }
}
public class HumanlikeBreastfeed : Breastfeed
{
    public bool OverseerBreastfeed;
    public HumanlikeBreastfeed()
    {
        this.AllowBreastfeeding = true;
        this.BreastfeedHumanlike = true;
        this.BreastfeedAnimal = false;
        this.BreastfeedMechanoid = false;
        this.OverseerBreastfeed = false;
    }
    public HumanlikeBreastfeed(bool allowBreastfeeding, bool breastfeedHumanlike, bool breastfeedAnimal, bool breastfeedMechanoid, bool OverseerBreastfeed, bool allowForcedBreastfeed, bool allowForcedSuckle)
    {
        this.AllowBreastfeeding = allowBreastfeeding;
        this.BreastfeedHumanlike = breastfeedHumanlike;
        this.BreastfeedAnimal = breastfeedAnimal;
        this.BreastfeedMechanoid = breastfeedMechanoid;
        this.OverseerBreastfeed = OverseerBreastfeed;
    }
    public override void ExposeData()
    {
        Scribe_Values.Look<bool>(ref this.AllowBreastfeeding, "AllowBreastfeeding", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedHumanlike, "BreastfeedHumanlike", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedAnimal, "BreastfeedAnimal", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedMechanoid, "BreastfeedMechanoid", false, false);
        Scribe_Values.Look<bool>(ref this.OverseerBreastfeed, "OverseerBreastfeed", false, false);
    }
}
public class AnimalBreastfeed : Breastfeed
{
    public float BabyAge;
    public AnimalBreastfeed()
    {
        this.AllowBreastfeeding = true;
        this.BreastfeedHumanlike = false;
        this.BreastfeedAnimal = true;
        this.BreastfeedMechanoid = false;
        this.BabyAge = 0.2f;
    }
    public AnimalBreastfeed(bool allowBreastfeeding, bool breastfeedHumanlike, bool breastfeedAnimal, bool breastfeedMechanoid, float babyAge)
    {
        this.AllowBreastfeeding = allowBreastfeeding;
        this.BreastfeedHumanlike = breastfeedHumanlike;
        this.BreastfeedAnimal = breastfeedAnimal;
        this.BreastfeedMechanoid = breastfeedMechanoid;
        this.BabyAge = babyAge;
    }
    public override void ExposeData()
    {
        Scribe_Values.Look<bool>(ref this.AllowBreastfeeding, "AllowBreastfeeding", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedHumanlike, "BreastfeedHumanlike", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedAnimal, "BreastfeedAnimal", true, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedMechanoid, "BreastfeedMechanoid", false, false);
        Scribe_Values.Look<float>(ref this.BabyAge, "BabyAge", 1f, false);
    }

}
public class MechanoidBreastfeed : Breastfeed
{
    public float BabyAge;
    public MechanoidBreastfeed()
    {
        this.AllowBreastfeeding = false;
        this.BreastfeedHumanlike = false;
        this.BreastfeedAnimal = false;
        this.BreastfeedMechanoid = true;
        this.BabyAge = 99999f;
    }
    public MechanoidBreastfeed(bool allowBreastfeeding, bool breastfeedHumanlike, bool breastfeedAnimal, bool breastfeedMechanoid, bool suckleOverseer, float babyAge)
    {
        this.AllowBreastfeeding = allowBreastfeeding;
        this.BreastfeedHumanlike = breastfeedHumanlike;
        this.BreastfeedAnimal = breastfeedAnimal;
        this.BreastfeedMechanoid = breastfeedMechanoid;
        this.BabyAge = babyAge;
    }
    public override void ExposeData()
    {
        Scribe_Values.Look<bool>(ref this.AllowBreastfeeding, "AllowBreastfeeding", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedHumanlike, "BreastfeedHumanlike", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedAnimal, "BreastfeedAnimal", false, false);
        Scribe_Values.Look<bool>(ref this.BreastfeedMechanoid, "BreastfeedMechanoid", false, false);
        Scribe_Values.Look<float>(ref this.BabyAge, "BabyAge", 99999f, false);
    }
}

