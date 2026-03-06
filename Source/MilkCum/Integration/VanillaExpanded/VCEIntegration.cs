using HarmonyLib;
using Verse;
using System;
using System.Collections.Generic;
using Verse.AI;
using System.Linq;

namespace MilkCum.Integration.VanillaExpanded;

[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly HarmonyLib.Harmony Harmony;

    static ApplyPatches()
    {
        Harmony = new HarmonyLib.Harmony("com.akaster.rimworld.mod.equalmilking.vme_harmonypatch");
        Log.Message("[MilkCum]: Vanilla Milk Expanded Loaded, Patching...");

        var boxType = AccessTools.TypeByName("CompAssignableToPawn_Box");
        if (boxType != null)
        {
            var getter = AccessTools.Property(boxType, "AssigningCandidates")?.GetGetMethod();
            if (getter != null)
            {
                Harmony.Patch(getter, prefix: new HarmonyMethod(typeof(CompAssignableToPawn_Box_Patch), nameof(CompAssignableToPawn_Box_Patch.AssigningCandidates_Prefix)));
            }
        }

        var layMilkType = AccessTools.TypeByName("JobGiver_LayMilk");
        if (layMilkType != null)
        {
            var tryGiveJob = AccessTools.Method(layMilkType, "TryGiveJob");
            if (tryGiveJob != null)
            {
                Harmony.Patch(tryGiveJob, prefix: new HarmonyMethod(typeof(JobGiver_LayMilk_Patch), nameof(JobGiver_LayMilk_Patch.TryGiveJob_Prefix)));
            }
        }
    }
}

public static class CompAssignableToPawn_Box_Patch
{
    public static bool AssigningCandidates_Prefix(object __instance, ref IEnumerable<Pawn> __result)
    {
        try
        {
            var parentField = AccessTools.Field(__instance.GetType(), "parent");
            var parent = parentField?.GetValue(__instance) as Thing;
            if (parent == null)
            {
                __result = Enumerable.Empty<Pawn>();
                return false;
            }
            if (!parent.Spawned || parent.Map == null)
            {
                __result = Enumerable.Empty<Pawn>();
                return false;
            }
            IEnumerable<Pawn> enumerable;
            bool bedHumanlike = parent.def?.building?.bed_humanlike ?? false;
            var canAssignTo = AccessTools.Method(__instance.GetType(), "CanAssignTo", new[] { typeof(Pawn) });
            var ideoligionForbids = AccessTools.Method(__instance.GetType(), "IdeoligionForbids", new[] { typeof(Pawn) });
            if (!bedHumanlike)
            {
                enumerable = ListerAsync.AllColonyPawns(parent.Map).Where(p => p != null && p.IsMilkable() && p.IsLactating());
            }
            else
            {
                enumerable = parent.Map.mapPawns.FreeColonists.OrderByDescending(delegate (Pawn p)
                {
                    int num;
                    if (p == null || canAssignTo == null)
                    {
                        num = 0;
                    }
                    else
                    {
                        var accepted = (AcceptanceReport)canAssignTo.Invoke(__instance, new object[] { p });
                        if (!accepted.Accepted)
                        {
                            num = 0;
                        }
                        else
                        {
                            num = (ideoligionForbids != null && (bool)ideoligionForbids.Invoke(__instance, new object[] { p }) ? 0 : 1);
                        }
                    }
                    return num;
                }).ThenBy(p => p.LabelShort);
            }
            __result = enumerable;
            return false;
        }
        catch (Exception ex)
        {
            Verse.Log.Warning($"[MilkCum] CompAssignableToPawn_Box AssigningCandidates_Prefix: {ex.Message}");
            __result = Enumerable.Empty<Pawn>();
            return false;
        }
    }
}

public static class JobGiver_LayMilk_Patch
{
    public static bool TryGiveJob_Prefix(object __instance, ref Job __result, Pawn pawn)
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
