using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Hediffs;
using MilkCum.Integration.DubsBadHygiene;
using static RimWorld.ChildcareUtility;

namespace MilkCum.Fluids.Lactation.Helpers;

public static class ChildcareHelper
{
    public static bool CanBreastfeedNow(this Pawn mom, Pawn baby, out BreastfeedFailReason? reason)
    {
        if (!mom.CanBreastfeedEver(baby)) { reason = null; return false; }
        if (!baby.IsAdult())
        {
            if (CanMomBreastfeedBabyNow(mom, baby, out reason))
            {
                if (!mom.AllowedToBreastFeed(baby)) { reason = BreastfeedFailReason.BabyForbiddenToMom; return false; }
                return true;
            }
            return false;
        }
        else if (!mom.IsLactating())
        {
            reason = BreastfeedFailReason.MomNotEnoughMilk;
            return false;
        }
        if (!mom.AllowedToBreastFeed(baby)) { reason = BreastfeedFailReason.BabyForbiddenToMom; return false; }
        reason = null;
        return true;
    }
    public static bool SuckleFromLactatingPawn(Pawn baby, Pawn feeder, int delta = 1)
    {
        if (!baby.TryGetFoodOrEnergyNeed(out float wanted, out float maxLevel)) { return false; }

        var comp = feeder.CompEquallyMilkable();
        var lactatingHediff = feeder.LactatingHediffWithComps();
        var lactatingComp = lactatingHediff?.TryGetComp<HediffComp_EqualMilkingLactating>();

        if (comp == null || lactatingComp == null)
            return false;

        // 吸奶流速：按「被吸的那一侧」的满度与容量算挤出乳压（GetMilkingFlowRateForSingleSide），避免未吸的一侧影响吸奶时间
        float flowPerSecond = comp.GetMilkingFlowRateForSingleSide();
        float ratePerTick = flowPerSecond / 60f * (float)delta;

        float milkAmt = feeder.MilkAmount();
        // 营养→乳池 / 乳池→营养 统一 1:1：1 池单位 = 1 营养，吸出多少池就加多少饱食度（与 Need_Food 补丁一致）
        const float nutritionPerPoolUnit = 1f;
        float toDrainPool = Mathf.Min(ratePerTick, comp.Fullness);
        if (baby.needs?.food != null)
            toDrainPool = Mathf.Min(toDrainPool, wanted / nutritionPerPoolUnit);
        else if (baby.needs?.energy != null)
            toDrainPool = Mathf.Min(toDrainPool, (wanted * MilkCumSettings.nutritionToEnergyFactor) / nutritionPerPoolUnit);

        var drainedKeys = new List<string>();
        float actualDrained = comp.DrainForConsumeSingleSide(toDrainPool, drainedKeys);
        lactatingHediff.OnGatheredLetdownByKeys(drainedKeys);
        lactatingComp.SyncChargeFromPool();

        comp.breastfedAmount += actualDrained;

        float nutritionEquivalent = actualDrained * nutritionPerPoolUnit;
        if (actualDrained > 0f)
            MilkCumSettings.PoolTickLog($"吸奶 {feeder.Name} -> {baby.Name} 池-{actualDrained:F4} 营养+{nutritionEquivalent:F4}");
        if (baby.needs.food != null)
        {
            baby.needs.food.CurLevel = Mathf.Min(baby.needs.food.CurLevel + nutritionEquivalent, baby.needs.food.MaxLevel);
        }
        else if (baby.needs.energy != null)
        {
            baby.needs.energy.CurLevel = Mathf.Min(baby.needs.energy.CurLevel + nutritionEquivalent / MilkCumSettings.nutritionToEnergyFactor, baby.needs.energy.MaxLevel);
        }

        DubsBadHygieneIntegration.SatisfyThirst(baby, actualDrained);

        Caravan caravan = baby.GetCaravan();
        if (caravan != null && feeder.GetCaravan() == caravan)
            feeder.mindState.BreastfeedCaravan(baby, nutritionEquivalent / maxLevel);
        baby.ideo?.IncreaseIdeoExposureIfBabyTick(feeder.Ideo, 1);

        bool stillWant = (baby.needs.food != null ? baby.needs.food.CurLevel < maxLevel - 0.01f : (baby.needs.energy?.CurLevel ?? 0f) < maxLevel - 0.01f) && comp.Fullness > 0f;
        return stillWant;
    }
    internal static Toil Breastfeed(Pawn pawn, Pawn baby, Action readyForNextToil)
    {
        Toil toil = ToilMaker.MakeToil("Breastfeed");
        toil.initAction = delegate
        {
            baby.jobs.StartJob(MakeBabySuckleJob(pawn), JobCondition.InterruptForced);
            if (baby.CarriedBy == null)
            {
                baby.jobs.posture = PawnPosture.LayingInBed;
            }
        };
        toil.AddBreastfeedActions(pawn, baby, readyForNextToil);
        return toil;
    }
    internal static Toil ActiveSuckle(Pawn pawn, Pawn baby, Action readyForNextToil)
    {
        Toil toil = ToilMaker.MakeToil("SuckleFrom");
        toil.initAction = delegate
        {
            PawnUtility.ForceWait(pawn, 400, baby, true);
        };
        toil.AddBreastfeedActions(pawn, baby, readyForNextToil);
        toil.AddFinishAction(delegate
        {
            if (pawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        });
        return toil;
    }
    private static void AddBreastfeedActions(this Toil toil, Pawn pawn, Pawn baby, Action readyForNextToil)
    {
        toil.AddSuckleTickAction(pawn, baby, readyForNextToil);
        toil.AddFinishAction(BreastfeedFinishAction(pawn, baby));
        toil.WithProgressBar(TargetIndex.A, GetBreastfeedProgress(pawn, baby), false, -0.5f, false);
        toil.handlingFacing = true;
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.WithEffect(EffecterDefOf.Breastfeeding, TargetIndex.A);
        toil.WithEffect(pawn.MilkDef().ingestible.ingestEffectEat, TargetIndex.A);
        toil.FailOnDestroyedOrNull(TargetIndex.A);
    }
    private static Action BreastfeedFinishAction(Pawn pawn, Pawn baby)
    {
        return delegate
        {
            var comp = pawn.CompEquallyMilkable();
            // ????? Drain?breastfedAmount ?????1 ? = 1 ???amountFed ?????
            float amountFed = comp?.breastfedAmount ?? 0f;
            if (comp != null)
                comp.breastfedAmount = 0f;
            // 一次吸奶增加一天泌乳时间
            if (amountFed > 0f)
            {
                var lactatingComp = pawn.LactatingHediffWithComps()?.TryGetComp<HediffComp_EqualMilkingLactating>();
                lactatingComp?.AddRemainingDays(1f);
            }
            if (amountFed >= 1f - float.Epsilon)
            {
                baby.needs?.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedMe, pawn, null);
                pawn.needs?.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedBaby, baby, null);
                // 3.1 ?????? 1 ?????/???? EM ?????EM_NursedBy / EM_NursedSomeone??????????
                if (MilkCumDefOf.EM_NursedBy != null)
                    baby.needs?.mood?.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_NursedBy, pawn, null);
                if (MilkCumDefOf.EM_NursedSomeone != null)
                    pawn.needs?.mood?.thoughts.memories.TryGainMemory(MilkCumDefOf.EM_NursedSomeone, baby, null);
                ThingDef milkDef = pawn.MilkDef();
                if (milkDef.ingestible.outcomeDoers != null)
                {
                    foreach (IngestionOutcomeDoer ingestionOutcomeDoer in milkDef.ingestible.outcomeDoers)
                    {
                        ingestionOutcomeDoer.DoIngestionOutcome(baby, ThingMaker.MakeThing(milkDef), Mathf.CeilToInt(amountFed));
                    }
                }
            }
            if (baby.CurJobDef == JobDefOf.BabySuckle)
            {
                baby.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
            if (pawn.Downed && pawn.carryTracker.CarriedThing != null)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing thing, null);
            }
        };
    }
    private static Func<float> GetBreastfeedProgress(Pawn pawn, Pawn baby)
    {
        if (pawn.MilkDef().ingestible.CachedNutrition > 0)
        {
            return () => baby.needs?.food?.CurLevelPercentage ?? baby.needs.energy.CurLevelPercentage;
        }
        // 无 CachedNutrition 时用 breastfedAmount 表示进度（或池满度）
        return () => pawn.CompEquallyMilkable()?.breastfedAmount ?? 0f;
    }
    internal static bool IsMomsFault(this BreastfeedFailReason? reason)
    {
        if (reason == null) { return false; }
        return reason.Value switch
        {
            BreastfeedFailReason.MomNotEnoughMilk or
            BreastfeedFailReason.MomInMentalState or
            BreastfeedFailReason.MomBeingCarried or
            BreastfeedFailReason.MomNotLactating or
            BreastfeedFailReason.MomNotOnMap or
            BreastfeedFailReason.MomDead or
            BreastfeedFailReason.MomInIncompatibleFactionToHauler => true,
            _ => false,
        };
    }
}
