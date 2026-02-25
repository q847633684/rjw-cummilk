using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using EqualMilking.Helpers;

namespace EqualMilking;
public class JobDriver_ForcedBreastfeed : JobDriver_Breastfeed
{
	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		base.SetFinalizerJob(delegate (JobCondition condition)
		{
			if (!this.pawn.IsCarryingPawn(this.Baby))
			{
				return null;
			}
			return ChildcareUtility.MakeBringBabyToSafetyJob(this.pawn, this.Baby);
		});
		Toil feedingToil = ChildcareHelper.Breastfeed(this.pawn, this.Baby, ReadyForNextToil);
		if (this.Baby.CanCasuallyInteractNow(false, true) && this.Baby.CurJobDef?.casualInterruptible != false)
		{
			Toil jumpIfDownedOrDrafted = Toils_Jump.JumpIf(feedingToil, () => this.pawn.Downed || this.pawn.Drafted).FailOn(() => !this.pawn.IsCarryingPawn(this.Baby));
			yield return Toils_Jump.JumpIf(jumpIfDownedOrDrafted, () => this.pawn.IsCarryingPawn(this.Baby)).FailOn(() => !this.pawn.IsCarryingPawn(this.Baby) && (this.pawn.Downed || this.pawn.Drafted));
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch, false).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false, true, false);
			yield return jumpIfDownedOrDrafted;
		}
		else
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch, false).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		}
		yield return feedingToil;
		yield break;
	}
}