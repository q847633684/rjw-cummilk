using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MilkCum.Milk.Jobs;
public class JobDriver_EquallyMilk : JobDriver_Milk
{
    private float gatherProgress;
    private Sustainer milkSustainer;
    public Pawn Target => job.GetTarget(TargetIndex.A).Thing as Pawn;
    public Building_Milking MilkBuilding => job.GetTarget(TargetIndex.B).Thing as Building_Milking;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gatherProgress, "gatherProgress");
    }

    protected override CompHasGatherableBodyResource GetComp(Pawn animal)
    {
        return animal.CompEquallyMilkable();
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return base.TryMakePreToilReservations(errorOnFailed)
            && (MilkBuilding == null
            || pawn.Reserve(MilkBuilding, job, 1, -1, null, errorOnFailed));
    }
    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool isEntity = (this.job.GetTarget(TargetIndex.A).Thing as Pawn).IsEntity;
        if (isEntity)
        {
            this.AddFailCondition(() => !Target.IsOnHoldingPlatform);
        }
        else
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }
        bool isMilkingSelf = this.pawn == Target;
        Toil wait = new();
        if (!isMilkingSelf)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, true);
            wait.initAction = delegate
            {
                wait.actor.pather.StopDead();
                if (!isEntity)
                {
                    PawnUtility.ForceWait(Target, 15000, null, true, true);
                }
            };
        }
        else if (MilkBuilding != null)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell, true);
        }
        wait.WithEffect(EMDefOf.EM_Milk, TargetIndex.A);
        wait.tickAction = delegate
        {
            Pawn actor = wait.actor;
            actor.skills?.Learn(SkillDefOf.Animals, 0.13f);
            gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) + (MilkBuilding?.SpeedOffset() ?? 0f);
            if (!(gatherProgress >= WorkTotal))
            {
                if (MilkBuilding != null)
                {
                    if (this.milkSustainer == null || this.milkSustainer.Ended)
                    {
                        this.milkSustainer = SoundDefOf.Recipe_Surgery.TrySpawnSustainer(SoundInfo.InMap(Target, MaintenanceType.None));
                    }
                    this.milkSustainer.Maintain();
                }
                return;
            }
            Target.CompEquallyMilkable().Gathered(pawn);
            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        };
        wait.AddFinishAction(delegate
        {
            this.milkSustainer?.End();
            if (Target?.CurJobDef == JobDefOf.Wait_MaintainPosture)
            {
                Target.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        });
        if (!isEntity)
        {
            wait.FailOnDespawnedOrNull(TargetIndex.A);
            wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        }
        wait.AddEndCondition(delegate
        {

            CompEquallyMilkable comp = Target.CompEquallyMilkable();
            if (comp.ActiveAndFull)
            {
                return JobCondition.Ongoing;
            }

            return JobCondition.Incompletable;
        });
        wait.defaultCompleteMode = ToilCompleteMode.Never;
        wait.WithProgressBar(TargetIndex.A, () => this.gatherProgress / WorkTotal);
        wait.activeSkill = () => SkillDefOf.Animals;
        yield return wait;
    }
}