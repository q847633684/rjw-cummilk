using System;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using MilkCum.Core;
using static RimWorld.ChildcareUtility;

namespace MilkCum.Milk.Helpers;

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
        HediffComp_Chargeable hediffComp_Chargeable = feeder.LactatingHediff().TryGetComp<HediffComp_Chargeable>();
        if (!baby.TryGetFoodOrEnergyNeed(out float wanted, out float maxLevel)) { return false; }
        float toConsumeInTicks = Mathf.Min(maxLevel * ((float)delta) / EqualMilkingSettings.breastfeedTime);
        if (feeder.MilkDef().ingestible.CachedNutrition <= 0)
        {
            float nonNutritionConsumed = hediffComp_Chargeable.GreedyConsume(toConsumeInTicks);
            feeder.CompEquallyMilkable().breastfedAmount += nonNutritionConsumed * Constants.MILK_CHARGE_FACTOR;
            return nonNutritionConsumed != wanted
                && Mathf.Approximately(nonNutritionConsumed, toConsumeInTicks)
                && !Mathf.Approximately(feeder.CompEquallyMilkable().breastfedAmount * feeder.MilkAmount(), 1f);
        }
        float toConsume = Mathf.Min(toConsumeInTicks, wanted);
        float consumed = baby.TryConsumeBreastMilk(hediffComp_Chargeable, toConsume);
        Caravan caravan = baby.GetCaravan();
        if (caravan != null && feeder.GetCaravan() == caravan)
        {
            feeder.mindState.BreastfeedCaravan(baby, consumed / maxLevel);
        }
        Pawn_IdeoTracker ideo = baby.ideo;
        ideo?.IncreaseIdeoExposureIfBabyTick(feeder.Ideo, 1);
        feeder.CompEquallyMilkable().breastfedAmount += consumed * Constants.MILK_CHARGE_FACTOR;
        return consumed != wanted && Mathf.Approximately(consumed, toConsume);
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
            float amountFed = pawn.CompEquallyMilkable().breastfedAmount * pawn.MilkAmount();
            pawn.CompEquallyMilkable().breastfedAmount = 0f;
            if (amountFed >= 1f - float.Epsilon)
            {
                baby.needs?.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedMe, pawn, null);
                pawn.needs?.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedBaby, baby, null);
                // 3.1：哺乳/被哺乳记忆，便于社交与关系（带 other 的记忆可影响 opinion）
                if (EMDefOf.EM_NursedBy != null)
                    baby.needs?.mood?.thoughts.memories.TryGainMemory(EMDefOf.EM_NursedBy, pawn, null);
                if (EMDefOf.EM_NursedSomeone != null)
                    pawn.needs?.mood?.thoughts.memories.TryGainMemory(EMDefOf.EM_NursedSomeone, baby, null);
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
        return () => pawn.CompEquallyMilkable().breastfedAmount / (1f / pawn.MilkAmount());
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
