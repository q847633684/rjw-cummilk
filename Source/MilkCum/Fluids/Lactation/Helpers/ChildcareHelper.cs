using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Hediffs;
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

        // 吸奶流速与挤奶统一：GetMilkingFlowRate（挤出乳压 f² × letdown），再按池量与需求上限
        float flowPerSecond = comp.GetMilkingFlowRate(false, null);
        float ratePerTick = flowPerSecond / 60f * (float)delta;

        float milkAmt = feeder.MilkAmount();
        float amountNormalizer = milkAmt / 3f;
        float nutritionMilt = feeder.MilkDef().ingestible.CachedNutrition <= 0f ? 1f
            : feeder.MilkDef().ingestible.CachedNutrition / DefDatabase<ThingDef>.GetNamed("Milk").ingestible.CachedNutrition;
        if (nutritionMilt <= 0f) nutritionMilt = 1f;
        amountNormalizer *= nutritionMilt * 2f;

        float toDrainPool = Mathf.Min(ratePerTick, comp.Fullness);
        if (feeder.MilkDef().ingestible.CachedNutrition > 0f)
            toDrainPool = Mathf.Min(toDrainPool, wanted / amountNormalizer);

        var drainedKeys = new List<string>();
        float actualDrained = comp.DrainForConsumeSingleSide(toDrainPool, drainedKeys);
        lactatingHediff.OnGatheredLetdownByKeys(drainedKeys);
        lactatingComp.SyncChargeFromPool();

        comp.breastfedAmount += actualDrained;

        float nutritionEquivalent = actualDrained * amountNormalizer;
        if (baby.needs.food != null)
        {
            baby.needs.food.CurLevel += nutritionEquivalent;
        }
        else if (baby.needs.energy != null)
        {
            baby.needs.energy.CurLevel += nutritionEquivalent / MilkCumSettings.nutritionToEnergyFactor;
        }

        Caravan caravan = baby.GetCaravan();
        if (caravan != null && feeder.GetCaravan() == caravan)
            feeder.mindState.BreastfeedCaravan(baby, nutritionEquivalent / maxLevel);
        baby.ideo?.IncreaseIdeoExposureIfBabyTick(feeder.Ideo, 1);

        bool stillWant = feeder.MilkDef().ingestible.CachedNutrition <= 0f
            ? (baby.needs.food != null ? baby.needs.food.CurLevel < maxLevel - 0.01f : (baby.needs.energy?.CurLevel ?? 0f) < maxLevel - 0.01f) && comp.Fullness > 0f
            : (nutritionEquivalent < wanted && actualDrained >= toDrainPool * 0.99f);
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
        // ????? Drain?breastfedAmount ?????? 1 ????
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
