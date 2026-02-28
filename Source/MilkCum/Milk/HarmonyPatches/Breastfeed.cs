using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.HarmonyPatches;
public class FillTab_Delegate
{
    public static Pawn baby;
    /// <summary>
    /// Controls what to hide in auto feed ITab menu
    /// </summary>
    public static bool ShouldHideInAutoFeed(Pawn feeder)
    {
        if (feeder == null) { return true; }
        return baby == null
            || ((feeder.WorkTypeIsDisabled(WorkTypeDefOf.Childcare)
            || feeder.IsWorkTypeDisabledByAge(WorkTypeDefOf.Childcare, out int _)
            || feeder.DevelopmentalStage <= DevelopmentalStage.Baby)
            && !(feeder.IsLactating() && feeder.AllowedToAutoBreastFeed(baby)));
    }
    public static int RemoveAllHidden(List<Pawn> pawns)
    {
        return pawns.RemoveAll(ShouldHideInAutoFeed);
    }
}
[HarmonyPatch]
public static class FillTabPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(ITab_Pawn_Feeding), nameof(ITab_Pawn_Feeding.FillTab),
            new Type[] { typeof(Pawn), typeof(Rect), typeof(Vector2).MakeByRefType(), typeof(Vector2).MakeByRefType(), typeof(List<Pawn>) });
    }
    static void Prefix(Pawn baby)
    {
        FillTab_Delegate.baby = baby;
    }
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        MethodInfo original = AccessTools.Method(typeof(List<Pawn>), nameof(List<Pawn>.RemoveAll), new[] { typeof(Predicate<Pawn>) });
        MethodInfo hideInAutoFeed = AccessTools.Method(typeof(FillTab_Delegate), nameof(FillTab_Delegate.RemoveAllHidden));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(original))
            {
                codes[i] = new CodeInstruction(OpCodes.Call, hideInAutoFeed);
                codes.Insert(i, new CodeInstruction(OpCodes.Pop));// Pop the original delegate out of the evaluation stack, pass the list to the custom method
                break;
            }
        }
        return codes.AsEnumerable();
    }

}
public static class DrawRow_Delegate
{
    public static Pawn baby;
    public static Pawn feeder;
    public static TaggedString GetLabel(AutofeedMode autofeedMode)
    {
        if (autofeedMode == AutofeedMode.Childcare)
        {
            return feeder.ChildcareText(baby);
        }
        else
        {
            return autofeedMode.Translate();
        }
    }
}
[HarmonyPatch(typeof(ITab_Pawn_Feeding))]
public static class ITab_Pawn_Feeding_Patch
{
    /// <summary>
    /// Change label from Childcare to Breastfeed for appropriate feeder
    /// Should change labels for pawns that cannot do childcare but can breastfeed
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ITab_Pawn_Feeding.GenerateFloatMenuOption))]
    public static void GenerateFloatMenuOption_Postfix(ref FloatMenuOption __result, AutofeedMode setting, ITab_Pawn_Feeding.BabyFeederPair pair)
    {
        if (setting == AutofeedMode.Childcare)
        {
            __result.Label = pair.feeder.ChildcareText(pair.baby);
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ITab_Pawn_Feeding.DrawRow))]
    public static void DrawRow_Prefix(Pawn baby, Pawn feeder)
    {
        DrawRow_Delegate.baby = baby;
        DrawRow_Delegate.feeder = feeder;
    }
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ITab_Pawn_Feeding.DrawRow))]
    public static IEnumerable<CodeInstruction> DrawRow_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        MethodInfo original = AccessTools.Method(typeof(AutoBreastfeedModeExtension), nameof(AutoBreastfeedModeExtension.Translate));
        MethodInfo getLabel = AccessTools.Method(typeof(DrawRow_Delegate), nameof(DrawRow_Delegate.GetLabel));
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(original))
            {
                codes[i] = new CodeInstruction(OpCodes.Call, getLabel);
                break;
            }
        }
        return codes.AsEnumerable();
    }
}
[HarmonyPatch(typeof(Pawn_MindState))]
public static class Pawn_MindState_Patch
{
    /// <summary>
    /// Allow childcare for Pawns that doesn't have Developmental stage baby
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_MindState.AutofeedSetting))]
    public static bool AutofeedSetting_Prefix(Pawn_MindState __instance, ref AutofeedMode __result, Pawn feeder)
    {
        if (feeder == __instance.pawn) { __result = AutofeedMode.Never; return false; }
        if (!__instance.pawn.RaceProps.Humanlike && !__instance.pawn.IsAdult())
        {
            __result = __instance.autoFeeders.TryGetValue(feeder, AutofeedMode.Childcare);
            return false;
        }
        return true;
    }
    /// <summary>
    /// Allow assigning childcare to Pawns that can't do childcare
    /// TODO Clean up logic
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_MindState.SetAutofeeder))]
    public static bool SetAutofeeder_Prefix(Pawn_MindState __instance, Pawn feeder, AutofeedMode setting)
    {
        if (feeder.AllowedToAutoBreastFeed(__instance.pawn))
        {
            if (setting == AutofeedMode.Childcare)
            {
                __instance.autoFeeders.Remove(feeder);
            }
            else
            {
                __instance.autoFeeders.SetOrAdd(feeder, setting);
            }
        }
        if (feeder.workSettings == null) // Prevent null reference exception, and try to be compatible with other mods that makes animal working
        {
            return false;
        }
        if (feeder.IsColonyMech) // Prevent messaging mech is not allowed to childcare
        {
            return false;
        }
        return true;
    }

}
//Allow non-humanlike to breastfeed
[HarmonyPatch(typeof(ChildcareUtility))]
public static class ChildcareUtility_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ChildcareUtility.CanFeed))]
    public static void CanFeed_PostFix(Pawn mom, ref bool __result, ref ChildcareUtility.BreastfeedFailReason? reason)
    {
        if (!mom.IsLactating())
        {
            reason = ChildcareUtility.BreastfeedFailReason.MomNotLactating;
            __result = false;
            return;
        }
        if (reason == ChildcareUtility.BreastfeedFailReason.MomNotHumanLike)
        {
            reason = null;
            __result = true;
        }

    }
    /// <summary>
    /// This function is also used to determine who is a baby by vanilla. Don't bypass age check
    /// </summary>
    /// <param name="__result"></param>
    /// <param name="baby"></param>
    /// <param name="reason"></param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ChildcareUtility.CanSuckle))]
    public static void CanSuckle_PostFix(ref bool __result, Pawn baby, ref ChildcareUtility.BreastfeedFailReason? reason)
    {
        if (__result || reason == ChildcareUtility.BreastfeedFailReason.BabyNull || reason == ChildcareUtility.BreastfeedFailReason.BabyDead) { return; }
        if (reason == ChildcareUtility.BreastfeedFailReason.BabyNotHumanlike)
        {
            reason = null;
            __result = true;
            if (baby.IsAdult())
            {
                reason = ChildcareUtility.BreastfeedFailReason.BabyTooOld;
                __result = false;
            }
        }
        else if (reason == ChildcareUtility.BreastfeedFailReason.BabyTooOld)
        {
            if (!baby.IsAdult())
            {
                reason = null;
                __result = true;
            }
        }
        if (baby.IsShambler)
        {
            reason = ChildcareUtility.BreastfeedFailReason.BabyShambler;
            __result = false;
        }

    }
    // Allow finding non-humanlike babies
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ChildcareUtility.FindAutofeedBaby))]
    public static IEnumerable<CodeInstruction> FindAutofeedBaby_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        MethodInfo original = AccessTools.Method(typeof(MapPawns), nameof(MapPawns.FreeHumanlikesOfFaction), new[] { typeof(Faction) });
        MethodInfo getBreastfeedablePawns = AccessTools.Method(typeof(EqualMilkingSettings), nameof(EqualMilkingSettings.GetAutoBreastfeedablePawnsList));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(original))
            {
                codes[i] = new CodeInstruction(OpCodes.Call, getBreastfeedablePawns);
                codes.RemoveAt(i - 4);//Remove unnecessary gets
                codes.RemoveAt(i - 4);
                codes.RemoveAt(i - 4);
                codes.RemoveAt(i - 4);
                //codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));// Pass the mom to the custom method
                break;
            }
        }
        return codes.AsEnumerable();
    }
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ChildcareUtility.CanFeedBaby))] //TODO Skip if fed by food
    public static bool CanFeedBaby_Prefix(ref bool __result, Pawn feeder, Pawn baby, ref ChildcareUtility.BreastfeedFailReason? reason)
    {
        if (!feeder.AllowedToBreastFeed(baby))
        {
            reason = ChildcareUtility.BreastfeedFailReason.BabyForbiddenToMom;
            __result = false;
            return false;
        }
        return true;
    }
    /// <summary>
    /// Allow suckle to gain energy instead of nutrition, bypass vanilla suckle
    /// </summary>
    /// <param name="__result"></param>
    /// <param name="baby"></param>
    /// <param name="feeder"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ChildcareUtility.SuckleFromLactatingPawn))]
#if v1_5
    public static bool SuckleFromLactatingPawn_Prefix(ref bool __result, Pawn baby, Pawn feeder)
    {
        __result = ChildcareHelper.SuckleFromLactatingPawn(baby, feeder);
        return false;
    }
#else
    public static bool SuckleFromLactatingPawn_Prefix(ref bool __result, Pawn baby, Pawn feeder, int delta)
    {
        __result = ChildcareHelper.SuckleFromLactatingPawn(baby, feeder, delta);
        return false;
    }
#endif
    /// <summary>
    /// Avoid not lactating not player facing
    /// </summary>
    /// <param name="reason"></param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ChildcareUtility.Translate))]
    public static void Translate_Prefix(ref ChildcareUtility.BreastfeedFailReason reason)
    {
        if (reason == ChildcareUtility.BreastfeedFailReason.MomNotLactating)
        {
            reason = ChildcareUtility.BreastfeedFailReason.MomNotEnoughMilk;
        }
    }
}
#if v1_5
    [HarmonyPatch(typeof(TargetingParameters))]
    public static class TargetingParameters_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TargetingParameters.ForBabyCare))]
        public static bool ForBabyCare_Prefix(ref TargetingParameters __result)
        {
            __result = new TargetingParameters
            {
                canTargetLocations = false,
                canTargetSelf = false,
                canTargetPawns = true,
                canTargetFires = false,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetAnimals = EqualMilkingSettings.animalBreastfeed.AllowBreastfeeding,
                canTargetHumans = EqualMilkingSettings.humanlikeBreastfeed.AllowBreastfeeding,
                canTargetMechs = EqualMilkingSettings.mechanoidBreastfeed.AllowBreastfeeding,
                canTargetPlants = false
            };
            return false;
        }

    }
#endif
[HarmonyPatch(typeof(JobDriver_Breastfeed))]
public static class JobDriver_Breastfeed_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(JobDriver_Breastfeed.Breastfeed))]
    public static bool Breastfeed_Prefix(ref Toil __result, JobDriver_Breastfeed __instance)
    {
        __result = ChildcareHelper.Breastfeed(__instance.pawn, __instance.Baby, __instance.ReadyForNextToil);
        return false;
    }
}
[HarmonyPatch(typeof(WorkGiver_Breastfeed))]
public static class WorkGiver_Breastfeed_Patch
{
    // Enable Autofeed mechanoids
    [HarmonyPostfix]
    [HarmonyPatch(nameof(WorkGiver_Breastfeed.PotentialWorkThingsGlobal))]
    public static void PotentialWorkThingsGlobal_Postfix(ref IEnumerable<Thing> __result, Pawn pawn)
    {
        if (EqualMilkingSettings.humanlikeBreastfeed.BreastfeedMechanoid
        || EqualMilkingSettings.humanlikeBreastfeed.OverseerBreastfeed
        || EqualMilkingSettings.animalBreastfeed.BreastfeedMechanoid
        || EqualMilkingSettings.mechanoidBreastfeed.BreastfeedMechanoid)
        {
            __result = __result.Concat(pawn.Map.mapPawns.SpawnedColonyMechs.Where(x => x.needs?.energy?.CurLevelPercentage < 0.2f));
        }
    }
}
[HarmonyPatch(typeof(HediffComp_Chargeable))]
public static class HediffComp_Chargeable_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HediffComp_Chargeable.GreedyConsume))]
    public static bool GreedyConsume_Prefix(HediffComp_Chargeable __instance, ref float __result, float desiredCharge)
    {
        if (__instance is HediffComp_EqualMilkingLactating lactating && __instance.Pawn.IsMilkable())
        {
            float amountNormalizer = __instance.Pawn.MilkAmount() / 3f; //Milk amount normalized to human lactating speed
            float nutritionMilt = __instance.Pawn.MilkDef().ingestible.CachedNutrition / DefDatabase<ThingDef>.GetNamed("Milk").ingestible.CachedNutrition; //Normalize to milk nutrition
            if (nutritionMilt == 0) { nutritionMilt = 1f; }// Drugs as milk
            amountNormalizer *= nutritionMilt;
            amountNormalizer /= 8f; //Charge is 8x vanilla, 0.125 max, 0.31 min
            desiredCharge /= amountNormalizer;
            float num;
            if (desiredCharge >= __instance.Charge)
            {
                num = __instance.Charge;
                lactating.SetMilkFullness(0f);
            }
            else
            {
                num = desiredCharge;
                lactating.SetMilkFullness(__instance.Charge - num);
            }
            __result = num * amountNormalizer;
            return false;
        }
        return true;
    }
}
