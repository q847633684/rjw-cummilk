#if v1_6
using System.Collections.Generic;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.UI;

public class FloatMenuOptionProvider_Breastfeed : FloatMenuOptionProvider
{
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;
    protected override bool CanSelfTarget => false;
    protected override bool MechanoidCanDo => false;
    public override bool CanTargetDespawned => true;
    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        if (!base.TargetThingValid(thing, context)) { return false; }
        if (!context.FirstSelectedPawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly)) { return false; }
        return true;
    }
    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        return clickedThing.BreastfeedMenuOptions(context.FirstSelectedPawn);
    }
}
#endif