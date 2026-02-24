using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace EqualMilking.HarmonyPatches;

/// <summary>分配进食任务时排除不允许食用的奶制品。</summary>
[HarmonyPatch]
public static class WorkGiver_Ingest_MilkProductFilter
{
    static System.Reflection.MethodBase TargetMethod()
    {
        var t = AccessTools.TypeByName("RimWorld.WorkGiver_Ingest");
        if (t == null) return null;
        return AccessTools.Method(t, "JobOnThing");
    }

    [HarmonyPrefix]
    static bool Prefix(Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (pawn == null || t == null) return true;
        if (pawn.CanConsumeMilkProduct(t)) return true;
        __result = null;
        return false; // skip original so no job is created for this thing
    }
}

/// <summary>进食开始时再次校验：若食物带 CompShowProducer.producer 且食用者不在允许范围内，则结束进食任务。</summary>
[HarmonyPatch]
public static class JobDriver_Ingest_MilkProductCheck
{
    static System.Reflection.MethodBase TargetMethod()
    {
        var t = AccessTools.TypeByName("RimWorld.JobDriver_Ingest");
        if (t == null) t = AccessTools.TypeByName("Verse.AI.JobDriver_Ingest");
        if (t == null) return null;
        return AccessTools.Method(t, "Notify_Start");
    }

    [HarmonyPostfix]
    static void Postfix(JobDriver __instance)
    {
        if (__instance?.pawn == null || __instance.job == null) return;
        Thing food = __instance.job.GetTarget(TargetIndex.A).Thing;
        if (food == null) return;
        if (__instance.pawn.CanConsumeMilkProduct(food)) return;
        __instance.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
    }
}
