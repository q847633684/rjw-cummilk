using Verse;

namespace MilkCum.Fluids.Lactation.Data;
public class RaceMilkType : IExposable
{
    public static bool defaultIsMilkable = true; //Magic
    public static string defaultMilkTypeDefName = "Milk";
    public bool isMilkable;
    public string milkTypeDefName;
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
    }
}
