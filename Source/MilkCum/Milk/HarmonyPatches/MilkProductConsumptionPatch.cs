using System.Reflection;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Milk.Comps;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace MilkCum.Milk.HarmonyPatches;

/// <summary>分配进食任务时排除不允许食用的奶制品。目标方法在 1.6 可能在不同程序集，故不参与 PatchAll，由 ApplyOptionalPatches 在找到时再打补丁。</summary>
public static class WorkGiver_Ingest_MilkProductFilter
{
    static MethodBase GetTargetMethod()
    {
        foreach (var typeName in new[] { "RimWorld.WorkGiver_Ingest", "Verse.AI.WorkGiver_Ingest" })
        {
            var t = AccessTools.TypeByName(typeName);
            if (t == null) continue;
            var m = AccessTools.Method(t, "JobOnThing");
            if (m != null) return m;
        }
        return null;
    }

    public static void ApplyOptionalPatches(HarmonyLib.Harmony harmony)
    {
        var target = GetTargetMethod();
        if (target == null) return;
        var prefix = AccessTools.Method(typeof(WorkGiver_Ingest_MilkProductFilter), nameof(Prefix));
        harmony.Patch(target, prefix: new HarmonyLib.HarmonyMethod(prefix));
    }

    static bool Prefix(Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (pawn == null || t == null) return true;
        if (pawn.CanConsumeMilkProduct(t)) return true;
        __result = null;
        return false; // skip original so no job is created for this thing
    }
}

/// <summary>进食开始时再次校验：若食物带 CompShowProducer.producer 且食用者不在允许范围内，则结束进食任务。目标在 1.6 可能在不同程序集，可选应用。</summary>
public static class JobDriver_Ingest_MilkProductCheck
{
    static MethodBase GetTargetMethod()
    {
        var t = AccessTools.TypeByName("RimWorld.JobDriver_Ingest");
        if (t == null) t = AccessTools.TypeByName("Verse.AI.JobDriver_Ingest");
        if (t == null) return null;
        return AccessTools.Method(t, "Notify_Start");
    }

    public static void ApplyOptionalPatches(HarmonyLib.Harmony harmony)
    {
        var target = GetTargetMethod();
        if (target == null) return;
        var postfix = AccessTools.Method(typeof(JobDriver_Ingest_MilkProductCheck), nameof(Postfix));
        harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
    }

    static void Postfix(JobDriver __instance)
    {
        if (__instance?.pawn == null || __instance.job == null) return;
        Thing food = __instance.job.GetTarget(TargetIndex.A).Thing;
        if (food == null) return;
        if (__instance.pawn.CanConsumeMilkProduct(food)) return;
        __instance.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
    }
}

/// <summary>7.5：食用者吃完带产主的奶/精液制品后，若食用者是产主的伴侣，给产主轻微正面记忆。</summary>
[HarmonyPatch(typeof(ThingComp), nameof(ThingComp.PostIngested))]
public static class Patch_PostIngested_PartnerAteMyProduct
{
    [HarmonyPostfix]
    static void Postfix(ThingComp __instance, Pawn ingester)
    {
        if (ingester == null || __instance?.parent == null) return;
        var comp = __instance.parent.TryGetComp<CompShowProducer>();
        if (comp?.producer == null || comp.producer == ingester) return;
        if (!comp.producer.relations?.DirectRelationExists(PawnRelationDefOf.Lover, ingester) ?? true) return;
        comp.producer.needs?.mood?.thoughts?.memories?.TryGainMemory(EMDefOf.EM_PartnerAteMyProduct);
    }
}
