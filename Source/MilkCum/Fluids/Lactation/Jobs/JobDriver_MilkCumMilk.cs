using System.Collections.Generic;
using MilkCum.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MilkCum.Fluids.Lactation.Jobs;
public class JobDriver_MilkCumMilk : JobDriver_Milk
{
    private const float EPS = 0.001f;
    /// <summary>本场次目标取量（整瓶数，池单位）</summary>
    private float amountToTake;
    /// <summary>已取量累计（池单位）</summary>
    private float totalDrained;
    private Sustainer milkSustainer;
    private readonly List<string> drainedKeys = new();
    public Pawn Target => job.GetTarget(TargetIndex.A).Thing as Pawn;
    public Building_Milking MilkBuilding => job.GetTarget(TargetIndex.B).Thing as Building_Milking;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref amountToTake, "amountToTake", 0f);
        Scribe_Values.Look(ref totalDrained, "totalDrained", 0f);
    }

    protected override CompHasGatherableBodyResource GetComp(Pawn animal)
    {
        return animal.CompEquallyMilkable();
    }

    /// <summary>初始化本场次目标取量（整瓶数）；不足 1 瓶时 amountToTake=0，由 initAction 结束为 Incompletable。</summary>
    private void InitMilkingSession()
    {
        CompEquallyMilkable comp = Target?.CompEquallyMilkable();
        if (comp == null) return;
        float whole = Mathf.Floor(comp.Fullness);
        amountToTake = whole >= 1f ? whole : 0f;
        totalDrained = 0f;
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
                InitMilkingSession();
                if (amountToTake < 1f)
                {
                    wait.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
            };
        }
        else if (MilkBuilding != null)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell, true);
            wait.initAction = delegate
            {
                InitMilkingSession();
                if (amountToTake < 1f)
                {
                    wait.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
            };
        }
        else
        {
            wait.initAction = delegate
            {
                InitMilkingSession();
                if (amountToTake < 1f)
                {
                    wait.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
            };
        }
        wait.WithEffect(MilkCumDefOf.EM_Milk, TargetIndex.A);
        wait.tickAction = delegate
        {
            Pawn actor = wait.actor;
            CompEquallyMilkable comp = Target?.CompEquallyMilkable();
            if (comp == null)
            {
                actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }
            if (amountToTake <= 0f)
            {
                if (totalDrained > 0f)
                {
                    comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                    totalDrained = 0f;
                }
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                return;
            }
            actor.skills?.Learn(SkillDefOf.Animals, 0.13f);
            float remaining = amountToTake - totalDrained;
            drainedKeys.Clear();
            float actualDrained;
            if (MilkBuilding != null)
            {
                // 机器挤奶：每侧按自身流速独立排空，并行进行；总耗时由最慢的一侧决定（如左 2.5 秒、右 1 秒 → 总时间 2.5 秒）
                var flowRatesPerSide = comp.GetMilkingFlowRatesPerSide(true, MilkBuilding);
                var ratePerSidePerTick = new List<float>(flowRatesPerSide.Count);
                for (int i = 0; i < flowRatesPerSide.Count; i++)
                    ratePerSidePerTick.Add(flowRatesPerSide[i] / 60f);
                actualDrained = comp.DrainForConsumeParallel(ratePerSidePerTick, remaining, drainedKeys);
            }
            else
            {
                float flowPerSecond = comp.GetMilkingFlowRate(false, null);
                float ratePerTick = flowPerSecond / 60f;
                float drain = Mathf.Min(ratePerTick, remaining, comp.Fullness);
                if (drain <= 0f || totalDrained >= amountToTake)
                {
                    if (totalDrained > 0f)
                    {
                        comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                        totalDrained = 0f;
                    }
                    this.milkSustainer?.End();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return;
                }
                actualDrained = comp.DrainForConsume(drain, drainedKeys);
            }
            // 通知泌乳反射：本 tick 被扣量的池侧 key，用于更新喷乳反射 R 与流速倍率
            Target.LactatingHediffWithComps()?.OnGatheredLetdownByKeys(drainedKeys);
            Target.LactatingHediffComp()?.SyncChargeFromPool();
            totalDrained += actualDrained;
            if (MilkBuilding != null)
            {
                if (this.milkSustainer == null || this.milkSustainer.Ended)
                    this.milkSustainer = SoundDefOf.Recipe_Surgery.TrySpawnSustainer(SoundInfo.InMap(Target, MaintenanceType.None));
                this.milkSustainer.Maintain();
            }
            if (totalDrained >= amountToTake - EPS)
            {
                if (totalDrained > 0f)
                {
                    comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                    totalDrained = 0f;
                }
                this.milkSustainer?.End();
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        };
        wait.AddFinishAction(delegate
        {
            this.milkSustainer?.End();
            // 挤奶被打断时，已扣的池量仍产瓶，不丢失
            if (totalDrained > 0f && Target?.CompEquallyMilkable() is CompEquallyMilkable comp)
                comp.SpawnBottlesForDrainedAmount(totalDrained, pawn, MilkBuilding);
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
        wait.defaultCompleteMode = ToilCompleteMode.Never;
        wait.WithProgressBar(TargetIndex.A, () => amountToTake <= 0f ? 0f : Mathf.Clamp01(totalDrained / amountToTake));
        wait.activeSkill = () => SkillDefOf.Animals;
        yield return wait;
    }
}