using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using EqualMilking.Helpers;

namespace EqualMilking;
/// <summary>
/// Allow non-humanlike to breastfeed
/// </summary>
public class JobGiver_EqualBreastfeed : ThinkNode_JobGiver
{
	private readonly List<Pawn> tmpBabies = new();
	protected virtual IEnumerable<Pawn> Babies(Pawn pawn)
	{
		return pawn.Map.AllColonyPawns().Where(p => p != pawn && !p.IsAdult());
	}
	protected virtual bool IsValidFeederBasic(Pawn pawn)
	{
		return pawn.IsColonyPawn() && pawn.LactatingHediffComp()?.CanActivate == true;
	}
	protected virtual bool IsValidFeeder(Pawn pawn)
	{
		return pawn.AllowBreastFeeding();
	}
	protected virtual bool CanAutoFeed(Pawn mom, Pawn baby, out ChildcareUtility.BreastfeedFailReason? breastfeedFailReason)
	{
		if (!baby.IsConsideredHungryForMom(mom))
		{
			breastfeedFailReason = ChildcareUtility.BreastfeedFailReason.BabyNotHungry;
			return false;
		}
		if (!ChildcareUtility.CanMomBreastfeedBabyNow(mom, baby, out breastfeedFailReason)) { return false; }
		if (!ChildcareUtility.CanHaulBabyToMomNow(mom, mom, baby, false, out breastfeedFailReason)) { return false; }
		if (baby.mindState.AutofeedSetting(mom) == AutofeedMode.Never) { return false; }
		return true;
	}
	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.GetDefaultMilkSetting() == null) { return null; }
		if (!IsValidFeeder(pawn) || !IsValidFeederBasic(pawn)) { return null; }
		tmpBabies.Clear();
		foreach (Pawn baby in Babies(pawn))
		{
			if (!pawn.AllowedToAutoBreastFeed(baby)) { continue; }
			if (!CanAutoFeed(pawn, baby, out ChildcareUtility.BreastfeedFailReason? breastfeedFailReason))
			{
				if (breastfeedFailReason.IsMomsFault()) { break; }
				continue;
			}
			if (!pawn.CanReserve(pawn) || !pawn.CanReserveAndReach(baby, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				continue;
			}
			tmpBabies.Add(baby);
		}
		// Distance sort
		if (tmpBabies.Count > 0)
		{
			tmpBabies.SortBy(x => x.PositionHeld.DistanceToSquared(pawn.PositionHeld));
			Job job = JobMaker.MakeJob(EMDefOf.EM_ForcedBreastfeed, tmpBabies[0]);
			job.count = 1;
			return job;
		}
		return null;
	}
}