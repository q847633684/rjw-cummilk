using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.Givers;
/// <summary>
/// Allow non-humanlike to breastfeed
/// </summary>
public class JobGiver_ActiveSuckle : JobGiver_GetFood
{
    private readonly List<Pawn> tmpPawns = new();
    public override float GetPriority(Pawn pawn)
    {
        if (!pawn.IsColonyPawn()) { return 0f; }
        if (pawn.IsHungryOrLowEnergy() && !FoodUtility.ShouldBeFedBySomeone(pawn)) { return 9.8f; }
        return 0f;
    }
    protected override Job TryGiveJob(Pawn pawn)
    {
		if (pawn.GetDefaultMilkSetting() == null) { return null; }
        if (!pawn.IsColonyPawn() || !pawn.IsHungryOrLowEnergy() || !pawn.CanReserve(pawn, 1, -1, null, false)) { return null; }
        tmpPawns.Clear();
        foreach (Pawn colonyPawn in pawn.Map.AllColonyPawns())
        {
            if (!colonyPawn.AllowedToAutoBreastFeed(pawn))
            {
                continue;
            }
            if (colonyPawn.LactatingHediffComp() is HediffComp_EqualMilkingLactating lactatingHediff)
            {
                if (lactatingHediff.CanActivate
                    && colonyPawn.CanCasuallyInteractNow(false, true)
                    && pawn.DistanceTo(colonyPawn) <= 30f
                    && pawn.CanReserveAndReach(colonyPawn, PathEndMode.OnCell, Danger.Deadly))
                {
                    tmpPawns.Add(colonyPawn);
                }
            }
        }
        if (tmpPawns.Count > 0 && EMDefOf.EM_ActiveSuckle != null)
        {
            tmpPawns.SortBy(x => x.PositionHeld.DistanceToSquared(pawn.PositionHeld));
            return JobMaker.MakeJob(EMDefOf.EM_ActiveSuckle, tmpPawns[0]);
        }
        return null;
    }
}
