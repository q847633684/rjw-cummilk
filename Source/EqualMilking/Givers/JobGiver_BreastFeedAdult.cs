using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using RimWorld.Planet;
using EqualMilking.Helpers;

namespace EqualMilking;
/// <summary>
/// Allow non-humanlike to breastfeed
/// </summary>
public class JobGiver_BreastFeedAdult : JobGiver_EqualBreastfeed
{
    protected override IEnumerable<Pawn> Babies(Pawn pawn)
    {
        return pawn.Map.AllColonyPawns().Where(p => p != pawn && p.IsAdult());
    }
    protected override bool IsValidFeeder(Pawn pawn)
    {
        return true;
    }
    protected override bool CanAutoFeed(Pawn mom, Pawn baby, out ChildcareUtility.BreastfeedFailReason? breastfeedFailReason)
    {
        breastfeedFailReason = null;
		if (!baby.IsConsideredHungryForMom(mom))
        {
            breastfeedFailReason = ChildcareUtility.BreastfeedFailReason.BabyNotHungry;
            return false;
        }
        if (!(mom.LactatingHediffComp()?.Charge >= 0.65f))
        {
            breastfeedFailReason = ChildcareUtility.BreastfeedFailReason.MomNotEnoughMilk;
            return false;
        }
        if (mom.IsFormingCaravan())
        {
            breastfeedFailReason = ChildcareUtility.BreastfeedFailReason.MomNotOnMap;
            return false;
        }
        return baby.CanCasuallyInteractNow(false, true)
            && (baby.needs.food != null || baby.needs.energy != null)
            && mom.DistanceTo(baby) <= 30f;
    }
}
