using HarmonyLib;
using Verse;
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
        Log.Message("CompAssignableToPawn_Box_Patch");
        IEnumerable<Pawn> enumerable;
        if (!__instance.parent.Spawned)
        {
            enumerable = Enumerable.Empty<Pawn>();
        }
        else
        {
            if (!__instance.parent.def.building.bed_humanlike)
            {
                enumerable = ListerAsync.AllColonyPawns(__instance.parent.Map).Where(p => p.IsMilkable() && p.IsLactating());
            }
            else
            {
                enumerable = __instance.parent.Map.mapPawns.FreeColonists.OrderByDescending(delegate (Pawn p)
                {
                    int num;
                    if (!__instance.CanAssignTo(p).Accepted)
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
        }
        __result = enumerable;
        return false;
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
