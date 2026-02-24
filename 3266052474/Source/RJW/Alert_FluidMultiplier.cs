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
        return "Fluid Multiplier Zero";
    }

    public override TaggedString GetExplanation()
    {
        return "Pawn(s) have a breast fluid multiplier of 0. This will cause them to not produce any milk. You need to fix them using Dev tool => RJW: Edit parts tool. The button is located at the top of the health tab when dev mode is enabled.";
    }
}