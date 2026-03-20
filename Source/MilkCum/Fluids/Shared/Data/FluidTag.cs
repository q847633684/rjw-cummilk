using Verse;

namespace MilkCum.Fluids.Shared.Data;

/// <summary>体液标签（乳汁/精液等共享）。</summary>
public class FluidTag : IExposable
{
    public string defName = "Milk";
    public bool TagPawn;
    public bool TagRace;

    public FluidTag() { }

    public FluidTag(string defName, bool tagPawn = false, bool tagRace = false)
    {
        this.defName = defName ?? "Milk";
        TagPawn = tagPawn;
        TagRace = tagRace;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName", "Milk", false);
        Scribe_Values.Look(ref TagPawn, "TagPawn", false, false);
        Scribe_Values.Look(ref TagRace, "TagRace", false, false);
    }
}
