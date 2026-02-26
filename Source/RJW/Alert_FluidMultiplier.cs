using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EqualMilking.RJW;

public class Alert_FluidMultiplier : Alert
{
    public static HashSet<Pawn> Culprits = new();
    public override AlertReport GetReport()
    {
        return AlertReport.CulpritsAre(Culprits.ToList());
    }

    public override string GetLabel()
    {
        return "Fluid_Multiplier_Zero".Translate();
    }

    public override TaggedString GetExplanation()
    {
        return "EM.AlertLactatingButFluidZero".Translate();
    }
}