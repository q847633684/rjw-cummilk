using HarmonyLib;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using Verse;

namespace MilkCum.Harmony.Compatibility;

/// <summary>
/// 方案 A 轻量：当目标拥有 CompEquallyMilkable 且 ActiveAndFull 时，禁止 RJW 的 WorkGiver_MilkHuman 派发 MilkHuman，
/// 仅由本 mod 的 WorkGiver_MilkCumMilk 派发 JobDefOf.Milk + JobDriver_MilkCumMilk，统一走流速挤奶。
/// </summary>
public static class RjwMilkHumanWorkGiverPatch
{
    public static void ApplyPatches(HarmonyLib.Harmony harmony)
    {
        var gatherType = AccessTools.TypeByName("rjw.WorkGiver_GatherHumanBodyResources");
        var milkHumanType = AccessTools.TypeByName("rjw.WorkGiver_MilkHuman");
        if (gatherType == null || milkHumanType == null)
            return;

        var hasJobOnThing = AccessTools.Method(gatherType, "HasJobOnThing");
        if (hasJobOnThing == null)
            return;

        harmony.Patch(hasJobOnThing,
            prefix: new HarmonyMethod(typeof(RjwMilkHumanWorkGiverPatch), nameof(HasJobOnThing_Prefix)));
    }

    /// <summary>
    /// 若当前是 RJW 的挤奶 WorkGiver 且目标有本 mod 的 CompEquallyMilkable 且可挤，则不再派发 RJW 的 MilkHuman job。
    /// </summary>
    public static bool HasJobOnThing_Prefix(object __instance, Pawn pawn, Thing t, ref bool __result)
    {
        if (t is not Pawn target)
            return true;

        // 仅对 RJW 的 WorkGiver_MilkHuman 生效，不动其他 GatherHumanBodyResources 子类
        if (__instance.GetType().Name != "WorkGiver_MilkHuman")
            return true;

        var comp = target.CompEquallyMilkable();
        if (comp == null || !comp.ActiveAndFull)
            return true;

        __result = false;
        return false;
    }
}
