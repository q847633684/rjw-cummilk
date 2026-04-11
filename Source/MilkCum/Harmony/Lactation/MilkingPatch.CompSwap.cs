using HarmonyLib;
using MilkCum.Core;
using MilkCum.Fluids.Lactation.Comps;
using RimWorld;
using Verse;

namespace MilkCum.Harmony;

[HarmonyPatch(typeof(CompMilkable))]
public static class CompMilkable_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch("Active", MethodType.Getter)]
    [HarmonyPriority(Priority.First)]
    public static bool Get_Active_Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(CompProperties_Milkable))]
[HarmonyPatch(MethodType.Constructor)]
public static class CompProperties_Milkable_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CompProperties_Milkable __instance)
    {
        __instance.compClass = typeof(CompEquallyMilkable);
    }
}

[HarmonyPatch(typeof(ThingWithComps))]
public static class ThingWithComps_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ThingWithComps.InitializeComps))]
    public static void InitializeComps_Post(ThingWithComps __instance)
    {
        __instance.CompEquallyMilkable();
    }
}
