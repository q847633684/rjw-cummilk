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
    /// <summary>按容量量化：本次挤奶所需工作量，由 MilkAmount 与池满度计算。</summary>
    private float workTotal = 400f;
    private Sustainer milkSustainer;
    public Pawn Target => job.GetTarget(TargetIndex.A).Thing as Pawn;
    public Building_Milking MilkBuilding => job.GetTarget(TargetIndex.B).Thing as Building_Milking;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gatherProgress, "gatherProgress");
        Scribe_Values.Look(ref workTotal, "workTotal", 400f);
    }

    protected override CompHasGatherableBodyResource GetComp(Pawn animal)
    {
        return animal.CompEquallyMilkable();
    }

    /// <summary>按容量量化：工作量 = 基准 × (1 + 容量系数×(MilkAmount-1)) × (0.5+0.5×池满度)，有上下限。</summary>
    private float GetWorkTotal()
    {
        CompEquallyMilkable comp = Target?.CompEquallyMilkable();
        if (comp == null)
            return EqualMilkingSettings.milkingWorkTotalBase;
        float milkAmt = Target.MilkAmount();
        float capMult = Mathf.Clamp(1f + EqualMilkingSettings.milkingCapacityFactor * (milkAmt - 1f), 0.5f, 2.5f);
        float fullMult = 0.5f + 0.5f * Mathf.Clamp01(comp.Fullness / Mathf.Max(0.01f, comp.maxFullness));
        return EqualMilkingSettings.milkingWorkTotalBase * capMult * fullMult;
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
                workTotal = GetWorkTotal();
            };
        }
        else if (MilkBuilding != null)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell, true);
            wait.initAction = delegate { workTotal = GetWorkTotal(); };
        }
        else
        {
            wait.initAction = delegate { workTotal = GetWorkTotal(); };
        }
        wait.WithEffect(EMDefOf.EM_Milk, TargetIndex.A);
        wait.tickAction = delegate
        {
            Pawn actor = wait.actor;
            actor.skills?.Learn(SkillDefOf.Animals, 0.13f);
            gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) + (MilkBuilding?.SpeedOffset() ?? 0f);
            if (!(gatherProgress >= workTotal))
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
        wait.WithProgressBar(TargetIndex.A, () => this.gatherProgress / workTotal);
        wait.activeSkill = () => SkillDefOf.Animals;
        yield return wait;
    }
}