using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
namespace MilkCum.Milk.HarmonyPatches;

[HarmonyPatch]
public static class WorkGiverPatches
{
    /// <summary>
    /// Childcare-capable mech/animal don't play with themselves as babies
    /// Patching JobOnThing to be compatible with Surrogate Mechanoid that uses JobOnThing directly
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_PlayWithBaby), nameof(WorkGiver_PlayWithBaby.JobOnThing))]
    [HarmonyPrefix]
    public static bool Play_JobOnThing_Prefix(Pawn pawn, Thing t, ref Job __result)
    {
        if (pawn == t)
        {
            __result = null;
            return false;
        }
        return true;
    }
    /// <summary>
    /// Prevent bottle feeding self
    /// Disable bottle feeding for non-breastfeedable
    /// </summary>
    /// <param name="pawn"></param>
    /// <param name="t"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(WorkGiver_FeedBabyManually), nameof(WorkGiver_FeedBabyManually.CanCreateManualFeedingJob))]
    [HarmonyPrefix]
    public static bool CanCreateManualFeedingJob_Prefix(Pawn pawn, Thing t, ref bool __result)
    {
        // Prevent feeding self
        if (pawn == t)
        {
            __result = false;
            return false;
        }
        // Prevent null reference and feeding random things. While leave as much room for other mods to add bottle feeding mechanisms as possible
        // TODO remove this and transpile/finalizer memory gain
        if (t is Pawn baby && (baby.needs?.mood?.thoughts?.memories == null
            || pawn.needs?.mood?.thoughts?.memories == null
            || baby.mindState.AutofeedSetting(pawn) == AutofeedMode.Never))
        {
            __result = false;
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.SafePlaceForBaby))]
    [HarmonyPrefix]
    public static bool SafePlaceForBaby_Prefix(Pawn baby, Pawn hauler, ref LocalTargetInfo __result)
    {
        if (baby == hauler)
        {
            __result = LocalTargetInfo.Invalid;
            return false;
        }
        return true;
    }
    /// <summary>
    /// Fix null reference when used with VEF
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(StorageSettings), nameof(StorageSettings.CopyFrom))]
    [HarmonyPrefix]
    public static bool CopyFrom_Prefix(StorageSettings __instance, StorageSettings other)
    {
        if (other == null || __instance == null)
        {
            return false;
        }
        return true;
    }
}