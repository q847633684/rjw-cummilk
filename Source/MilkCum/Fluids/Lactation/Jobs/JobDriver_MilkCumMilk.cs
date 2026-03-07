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
    /// <summary>流速驱动：本场次目标取量（整瓶数，池单位）</summary>
    private float amountToTake;
    /// <summary>流速驱动：已取量累计</summary>
    private float totalDrained;
    /// <summary>本场次时长（tick），用于算 ratePerTick = amountToTake / workTotal</summary>
    private float workTotal = 400f;
    private Sustainer milkSustainer;
    public Pawn Target => job.GetTarget(TargetIndex.A).Thing as Pawn;
    public Building_Milking MilkBuilding => job.GetTarget(TargetIndex.B).Thing as Building_Milking;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref amountToTake, "amountToTake", 0f);
        Scribe_Values.Look(ref totalDrained, "totalDrained", 0f);
        Scribe_Values.Look(ref workTotal, "workTotal", 400f);
    }

    protected override CompHasGatherableBodyResource GetComp(Pawn animal)
    {
        return animal.CompEquallyMilkable();
    }

    /// <summary>按容量量化：本场次时长（tick）= 基准 × capMult × fullMult ÷ (1+工具速度加成)；工具 SpeedOffset 使电动比手动更快。</summary>
    private float GetWorkTotal()
    {
        CompEquallyMilkable comp = Target?.CompEquallyMilkable();
        if (comp == null)
            return MilkCumSettings.milkingWorkTotalBase;
        float milkAmt = Target.MilkAmount();
        float capMult = Mathf.Clamp(1f + MilkCumSettings.milkingCapacityFactor * (milkAmt - 1f), 0.5f, 2.5f);
        float fullMult = 0.5f + 0.5f * Mathf.Clamp01(comp.Fullness / Mathf.Max(0.01f, comp.maxFullness));
        float baseWork = MilkCumSettings.milkingWorkTotalBase * capMult * fullMult;
        float speedMult = 1f + (MilkBuilding?.SpeedOffset() ?? 0f);
        return baseWork / Mathf.Max(0.01f, speedMult);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return base.TryMakePreToilReservations(errorOnFailed)
            && (MilkBuilding == null
            || pawn.Reserve(MilkBuilding, job, 1, -1, null, errorOnFailed));
    }
    /// <summary>流速驱动：初始化本场次目标取量、时长与速率</summary>
    private void InitMilkingSession()
    {
        CompEquallyMilkable comp = Target?.CompEquallyMilkable();
        if (comp == null) return;
        float whole = Mathf.Floor(comp.Fullness);
        amountToTake = whole >= 1f ? whole : 0f;
        workTotal = GetWorkTotal();
        totalDrained = 0f;
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
            };
        }
        else if (MilkBuilding != null)
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell, true);
            wait.initAction = delegate { InitMilkingSession(); };
        }
        else
        {
            wait.initAction = delegate { InitMilkingSession(); };
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
                    comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                return;
            }
            actor.skills?.Learn(SkillDefOf.Animals, 0.13f);
            float ratePerTick = amountToTake / Mathf.Max(0.01f, workTotal);
            float remaining = amountToTake - totalDrained;
            float drain = Mathf.Min(ratePerTick, remaining, comp.Fullness);
            if (drain <= 0f || totalDrained >= amountToTake)
            {
                if (totalDrained > 0f)
                    comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                this.milkSustainer?.End();
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                return;
            }
            var drainedKeys = new List<string>();
            float actualDrained = comp.DrainForConsume(drain, drainedKeys);
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
            if (totalDrained >= amountToTake - 0.001f)
            {
                if (totalDrained > 0f)
                    comp.SpawnBottlesForDrainedAmount(totalDrained, actor, MilkBuilding);
                this.milkSustainer?.End();
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
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
            if (totalDrained > 0f)
                return JobCondition.Ongoing;
            if (comp.ActiveAndFull)
                return JobCondition.Ongoing;
            return JobCondition.Incompletable;
        });
        wait.defaultCompleteMode = ToilCompleteMode.Never;
        wait.WithProgressBar(TargetIndex.A, () => amountToTake <= 0f ? 0f : Mathf.Clamp01(totalDrained / amountToTake));
        wait.activeSkill = () => SkillDefOf.Animals;
        yield return wait;
    }
}