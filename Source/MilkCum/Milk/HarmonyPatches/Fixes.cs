using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.HarmonyPatches;

[HarmonyPatch(typeof(HaulAIUtility))]
public static class HaulAIUtility_Patch
{
    /// <summary>
    /// Fix starting job 10 time error when trying to fill hopper with labeled human milk
    /// This fixes a missing check in vanilla code
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(HaulAIUtility.HaulToCellStorageJob))]
    public static bool HaulToCellStorageJob_Prefix(ref Job __result, Thing t, IntVec3 storeCell)
    {
        // Use safe try catch to avoid errors when other mods modify IsValidStorageFor
        try
        {
            if (t?.MapHeld == null) { return true; }
            if (!storeCell.IsValidStorageFor(t.MapHeld, t))
            {
                __result = null;
                return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}