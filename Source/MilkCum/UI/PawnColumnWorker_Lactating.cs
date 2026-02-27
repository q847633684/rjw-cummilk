using RimWorld;
using UnityEngine;
using Verse;
using MilkCum.Milk.Helpers;

namespace MilkCum.UI;
[StaticConstructorOnStartup]
public class PawnColumnWorker_Lactating : PawnColumnWorker_Icon
{
    private static readonly Texture2D icon = ContentFinder<Texture2D>.Get("ui/icons/lifestage/young", true);
    protected override Texture2D GetIconFor(Pawn pawn)
    {
        if (!pawn.IsLactating()) { return null; }
        return icon;
    }
    protected override Color GetIconColor(Pawn pawn)
    {
        Hediff lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        if (lactating == null) { return Color.white; }
        if (lactating.Severity < 1) { return Color.blue; }
        return Color.red;
    }
    protected override string GetIconTip(Pawn pawn)
    {
        Hediff lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        if (lactating.Severity < 1) { return Lang.Lactating + "\n" + "ConfigurableSeverity".Translate().Replace("{0}", lactating.Severity.ToString("F2")); }
        if (lactating.Severity >= 1) { return Lang.Lactating + "(" + Lang.Permanent + ")" + " x" + lactating.Severity.ToString("F0"); }
        return null;
    }
}
