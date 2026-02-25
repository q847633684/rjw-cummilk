using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using System.Linq;
using rjw;
using EqualMilking.Helpers;
namespace EqualMilking.RJW;
[StaticConstructorOnStartup]
internal static class ApplyPatches
{
    private static readonly Harmony Harmony;

    static ApplyPatches()
    {
        Harmony = new Harmony("com.akaster.rimworld.mod.equalmilking.rjw");
        Log.Message("[Equal Milking]: RJW Loaded, Patching...");
        if (AccessTools.TypeByName("SexFluidDef") == null)
        {
            Log.Warning("[Equal Milking]: RJW version too old, aborting.");
            return;
        }
        Harmony.PatchAll();
    }
}
[HarmonyPatch(typeof(ExtensionHelper))]
public static class CompAssignableToPawn_Box_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ExtensionHelper.MilkAmount))]
    public static void GetMilkAmount_Postfix(Pawn pawn, ref float __result)
    {
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (!breasts.EnumerableNullOrEmpty())
        {
            // Multiplier
            float multiplier = breasts.Select(b => b.GetPartComp().FluidMultiplier).Sum();
            if (multiplier == 0)
            {
                Alert_FluidMultiplier.Culprits.Add(pawn);
            }
            else
            {
                Alert_FluidMultiplier.Culprits.Remove(pawn);
            }
            __result *= multiplier;
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ExtensionHelper.MilkDef))]
    public static bool GetMilkProductDef_Prefix(Pawn pawn, ref ThingDef __result)
    {
        __result = null;
        IEnumerable<ISexPartHediff> breasts = pawn.GetBreasts();
        if (!breasts.EnumerableNullOrEmpty())
        {
            foreach (ISexPartHediff breast in breasts)
            {
                if (breast.GetPartComp()?.Fluid?.consumable is ThingDef consumable && consumable.defName != "Milk")
                {
                    __result = consumable;
                    return false;
                }
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(Hediff_BasePregnancy))]
public static class Hediff_BasePregnancy_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Hediff_BasePregnancy.PostBirth))]
    public static void PostBirth_Prefix(Pawn mother, Pawn baby)
    {
        if (mother.IsMilkable())
        {
            Hediff lactating = mother.health.GetOrAddHediff(HediffDefOf.Lactating);
            if (lactating != null)
            {
                lactating.Severity = Mathf.Max(lactating.Severity, 0.9999f);
            }
        }
        if (mother.IsInLactatingState() && mother.CompEquallyMilkable() is CompEquallyMilkable comp && mother.CanBreastfeedEver(baby))
        {
            // 谁可以使用我的奶：名单默认预填子女+伴侣
        }
    }
}
