using Verse;

namespace MilkCum.Milk.Data;
public class RaceMilkType : IExposable
{
    public static bool defaultIsMilkable = true; //Magic
    public static string defaultMilkTypeDefName = "Milk";
    public static int defaultMilkAmount = 3; //Human approximate milk amount, should be 2.xf but round up
    public bool isMilkable;
    public string milkTypeDefName;
    public int milkAmount;
    public ThingDef MilkTypeDef
    {
        get
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(milkTypeDefName) ?? DefDatabase<ThingDef>.GetNamed(defaultMilkTypeDefName);
            return def;
        }
    }
    public RaceMilkType()
    {
        this.isMilkable = defaultIsMilkable;
        this.milkTypeDefName = defaultMilkTypeDefName;
        this.milkAmount = defaultMilkAmount;
    }
    public void SetMilkType(ThingDef thingDef)
    {
        if (thingDef == null) return;
        this.milkTypeDefName = thingDef.defName;
    }
    public void ExposeData()
    {
        Scribe_Values.Look<bool>(ref isMilkable, "isMilkable", defaultIsMilkable, false);
        Scribe_Values.Look<string>(ref milkTypeDefName, "milkTypeDefName", defaultMilkTypeDefName, false);
        Scribe_Values.Look<int>(ref milkAmount, "milkAmount", defaultMilkAmount, false);
    }
}
