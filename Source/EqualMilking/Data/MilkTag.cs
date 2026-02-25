using Verse;

namespace EqualMilking.Data;
public class MilkTag : IExposable
{
    public string defName = "Milk";
    public bool TagPawn = false;
    public bool TagRace = false;

    public MilkTag()
    {
        this.defName = "Milk";
        this.TagPawn = false;
        this.TagRace = false;
    }
    public MilkTag(string defName)
    {
        this.defName = defName;
        this.TagPawn = false;
        this.TagRace = false;
    }
    public MilkTag(string defName, bool TagPawn, bool TagRace)
    {
        this.defName = defName;
        this.TagPawn = TagPawn;
        this.TagRace = TagRace;
    }
    public void ExposeData()
    {
        Scribe_Values.Look<string>(ref this.defName, "defName", "Milk", false);
        Scribe_Values.Look<bool>(ref this.TagPawn, "TagPawn", false, false);
        Scribe_Values.Look<bool>(ref this.TagRace, "TagRace", false, false);
    }

}