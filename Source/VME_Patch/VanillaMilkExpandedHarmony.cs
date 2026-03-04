using HarmonyLib;
using Verse;
using System;
using System.Collections.Generic;
using Verse.AI;
using System.Linq;
using EqualMilking.Helpers;
namespace EqualMilking.VME_HarmonyPatch;
[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly Harmony Harmony;

    static ApplyPatches()
    {
        Harmony = new Harmony("com.akaster.rimworld.mod.equalmilking.vme_harmonypatch");
        Log.Message("[Equal Milking]: Vanilla Milk Expanded Loaded, Patching...");
        Harmony.PatchAll();
    }
}
[HarmonyPatch(typeof(CompAssignableToPawn_Box))]
public static class CompAssignableToPawn_Box_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch("get_AssigningCandidates")]
    public static bool AssigningCandidates_Prefix(CompAssignableToPawn_Box __instance, ref IEnumerable<Pawn> __result)
    {
        try
        {
        if (__instance?.parent == null)
        {
            __result = Enumerable.Empty<Pawn>();
            return false;
        }
        if (!__instance.parent.Spawned || __instance.parent.Map == null)
        {
            __result = Enumerable.Empty<Pawn>();
            return false;
        }
        IEnumerable<Pawn> enumerable;
        bool bedHumanlike = __instance.parent.def?.building?.bed_humanlike ?? false;
        if (!bedHumanlike)
        {
            enumerable = ListerAsync.AllColonyPawns(__instance.parent.Map).Where(p => p != null && p.IsMilkable() && p.IsLactating());
        }
        else
        {
            enumerable = __instance.parent.Map.mapPawns.FreeColonists.OrderByDescending(delegate (Pawn p)
            {
                int num;
                if (p == null || !__instance.CanAssignTo(p).Accepted)
                {
                    num = 0;
                }
                else
                {
                    num = ((!__instance.IdeoligionForbids(p)) ? 1 : 0);
                }
                return num;
            }).ThenBy(p => p.LabelShort);
        }
        __result = enumerable;
        return false;
        }
        catch (Exception ex)
        {
            Verse.Log.Warning($"[Equal Milking] CompAssignableToPawn_Box AssigningCandidates_Prefix: {ex.Message}");
            __result = Enumerable.Empty<Pawn>();
            return false;
        }
    }

}
[HarmonyPatch(typeof(JobGiver_LayMilk))]
public static class JobGiver_LayMilk_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch("TryGiveJob")]
    public static bool TryGiveJob_Prefix(JobGiver_LayMilk __instance, ref Job __result, Pawn pawn)
    {
        if (!pawn.IsMilkable())
        {
            __result = null;
            return false;
        }
        if (pawn.CompEquallyMilkable().ActiveAndFull != true)
        {
            __result = null;
            return false;
        }
        return true;
    }
}
