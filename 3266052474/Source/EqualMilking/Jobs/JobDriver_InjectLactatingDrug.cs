using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
namespace EqualMilking;
[StaticConstructorOnStartup]
public class JobDriver_InjectLactatingDrug : JobDriver
{
	private const TargetIndex IngestableInd = TargetIndex.A;
	private const TargetIndex DelivereeInd = TargetIndex.B;
	protected Thing Food
	{
		get
		{
			return this.job.targetA.Thing;
		}
	}
	protected Pawn Deliveree
	{
		get
		{
			return (Pawn)this.job.targetB.Thing;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return this.pawn.Reserve(this.Deliveree, this.job, 1, -1, null, errorOnFailed, false);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (this.Deliveree.IsEntity) // Entity always despawned
		{
			this.AddFailCondition(() => !this.Deliveree.IsOnHoldingPlatform);
		}
		else
		{
			this.FailOnDespawnedNullOrForbidden(DelivereeInd);
		}
		if (this.pawn.inventory != null && this.pawn.inventory.Contains(base.TargetThingA))
		{
			yield return Toils_Misc.TakeItemFromInventoryToCarrier(this.pawn, IngestableInd);
		}
		else
		{
			yield return Toils_Goto.GotoThing(IngestableInd, PathEndMode.ClosestTouch, false).FailOnForbidden(IngestableInd);
			yield return Toils_Ingest.PickupIngestible(IngestableInd, this.Deliveree);
		}
		yield return Toils_Goto.GotoThing(DelivereeInd, PathEndMode.Touch, true);

		yield return InjectIngestible(this.Deliveree);
		yield return Toils_Ingest.FinalizeIngest(this.Deliveree, IngestableInd);
		yield break;
	}
	private Toil InjectIngestible(Pawn pawn)
	{
		Toil toil = ToilMaker.MakeToil("InjectIngestible");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Thing thing = actor.CurJob.GetTarget(IngestableInd).Thing;
			if (!thing.IngestibleNow)
			{
				pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
				return;
			}
			toil.actor.pather.StopDead();
			actor.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt((float)thing.def.ingestible.baseIngestTicks);
			if (thing.Spawned)
			{
				thing.Map.physicalInteractionReservationManager.Reserve(pawn, actor.CurJob, thing);
			}
			if (!pawn.IsEntity)
			{
				PawnUtility.ForceWait(pawn, actor.jobs.curDriver.ticksLeftThisToil, null, true, true);
			}
		};
#if v1_5
			toil.tickAction = delegate
			{
				if (pawn != toil.actor)
				{
					toil.actor.rotationTracker.FaceCell(pawn.Position);
				}
				else
				{
					Thing thing2 = toil.actor.CurJob.GetTarget(IngestableInd).Thing;
					if (thing2 != null && thing2.Spawned)
					{
						toil.actor.rotationTracker.FaceCell(thing2.Position);
					}
				}
				toil.actor.GainComfortFromCellIfPossible(false);
			};
#else
		toil.tickIntervalAction = delegate (int interval)
		{
			if (pawn != toil.actor)
			{
				toil.actor.rotationTracker.FaceCell(pawn.Position);
			}
			else
			{
				Thing thing2 = toil.actor.CurJob.GetTarget(IngestableInd).Thing;
				if (thing2 != null && thing2.Spawned)
				{
					toil.actor.rotationTracker.FaceCell(thing2.Position);
				}
			}
			toil.actor.GainComfortFromCellIfPossible(interval);
		};
#endif
		toil.WithProgressBar(IngestableInd, delegate
		{
			Thing thing3 = toil.actor.CurJob.GetTarget(IngestableInd).Thing;
			if (thing3 == null)
			{
				return 1f;
			}
			return 1f - (float)toil.actor.jobs.curDriver.ticksLeftThisToil / Mathf.Round((float)thing3.def.ingestible.baseIngestTicks);
		}, false, -0.5f, false);
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.FailOnDestroyedOrNull(IngestableInd);
		toil.AddFinishAction(delegate
		{
			Pawn chewer2 = pawn;
			Thing thing4;
			if (chewer2 == null)
			{
				thing4 = null;
			}
			else
			{
				Job curJob = chewer2.CurJob;
				thing4 = (curJob != null) ? curJob.GetTarget(IngestableInd).Thing : null;
			}
			Thing thing5 = thing4;
			if (thing5 == null)
			{
				return;
			}
			if (pawn.Map.physicalInteractionReservationManager.IsReservedBy(pawn, thing5))
			{
				pawn.Map.physicalInteractionReservationManager.Release(pawn, toil.actor.CurJob, thing5);
			}
		});
		toil.handlingFacing = true;
		Toils_Ingest.AddIngestionEffects(toil, pawn, IngestableInd, TargetIndex.None);
		return toil;
	}
}
