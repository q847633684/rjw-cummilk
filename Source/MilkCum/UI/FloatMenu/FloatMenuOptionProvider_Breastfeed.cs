#if v1_6
using System.Collections.Generic;
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
        // 仅当被点击者或当前选中者至少一方在泌乳时才显示母乳喂养菜单，避免非哺乳者也出现该选项
        if (thing is Pawn targetPawn && !targetPawn.IsLactating() && !context.FirstSelectedPawn.IsLactating())
            return false;
        return true;
    }
    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        return clickedThing.BreastfeedMenuOptions(context.FirstSelectedPawn);
    }
}
#endif