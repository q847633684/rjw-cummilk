using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using EqualMilking.Comps;

namespace EqualMilking.HarmonyPatches;

/// <summary>
/// When Cumpilation is loaded: tag cum items with producer so consumption
/// uses the same "who can eat" rules as milk (CompShowProducer + allowedConsumers).
/// Bucket can link to bed: only bed owner can use when empty; when occupied show "not your cum bucket"; shared room = everyone, mixed cum.
/// </summary>
public static class CumpilationIntegration
{
    internal static Pawn CumProducerForNextSpawn;

    public static void ApplyPatches(Harmony harmony)
    {
        // Cumpilation is merged into this mod; always apply producer/consumer integration.
        var jobGiverDeflateType = AccessTools.TypeByName("Cumpilation.Leaking.JobGiver_Deflate");
        if (jobGiverDeflateType != null)
        {
            var tryFindBucket = AccessTools.Method(jobGiverDeflateType, "TryFindBucketFor");
            if (tryFindBucket != null)
            {
                harmony.Patch(tryFindBucket, postfix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(JobGiver_Deflate_TryFindBucketFor_Postfix)));
            }
        }

        var extractType = AccessTools.TypeByName("Cumpilation.Leaking.Recipe_ExtractCum");
        if (extractType != null)
        {
            var spawnCum = AccessTools.Method(extractType, "SpawnCum");
            if (spawnCum != null)
            {
                harmony.Patch(spawnCum,
                    prefix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(ExtractCum_SpawnCum_Prefix)),
                    postfix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(ExtractCum_SpawnCum_Postfix)));
            }
        }

        var bucketType = AccessTools.TypeByName("Cumpilation.Leaking.JobDriver_DeflateBucket");
        if (bucketType != null)
        {
            var bucketSpawnCum = AccessTools.Method(bucketType, "SpawnCum");
            if (bucketSpawnCum != null)
            {
                harmony.Patch(bucketSpawnCum,
                    prefix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(DeflateBucket_SpawnCum_Prefix)),
                    postfix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(DeflateBucket_SpawnCum_Postfix)));
            }
        }

        var makeThing = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing), new[] { typeof(ThingDef) });
        if (makeThing != null)
        {
            harmony.Patch(makeThing, postfix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(ThingMaker_MakeThing_Postfix)));
        }

        // 自慰/性行为时“射进桶”的收集：精液直接生成在建筑格，产主 = 射精的小人
        var gatheringType = AccessTools.TypeByName("Cumpilation.Gathering.GatheringUtility");
        if (gatheringType != null)
        {
            var handleGatherer = AccessTools.Method(gatheringType, "HandleFluidGatherer");
            if (handleGatherer != null)
            {
                harmony.Patch(handleGatherer,
                    prefix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(Gathering_HandleFluidGatherer_Prefix)),
                    postfix: new HarmonyMethod(typeof(CumpilationIntegration), nameof(Gathering_HandleFluidGatherer_Postfix)));
            }
        }
    }

    static void Gathering_HandleFluidGatherer_Prefix(object gatherer, object props, int numberOfOtherBuildings)
    {
        CumProducerForNextSpawn = AccessTools.Field(props?.GetType(), "pawn")?.GetValue(props) as Pawn;
    }

    static void Gathering_HandleFluidGatherer_Postfix()
    {
        CumProducerForNextSpawn = null;
    }

    /// <summary>Only allow bucket if CompCumBucketLink.CanPawnUse(pawn); otherwise clear result and prompt "not your cum bucket" when occupied.</summary>
    static void JobGiver_Deflate_TryFindBucketFor_Postfix(Pawn pawn, ref Thing __result)
    {
        if (__result == null || pawn == null) return;
        var comp = __result.TryGetComp<CompCumBucketLink>();
        if (comp == null) return;
        if (!comp.CanPawnUse(pawn))
        {
            if (comp.ShouldWarnNotYourBucket(pawn))
                Messages.Message("EM_CumBucket_NotYours".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput);
            __result = null;
        }
    }

    static void ExtractCum_SpawnCum_Prefix(Pawn pawn)
    {
        CumProducerForNextSpawn = pawn;
    }

    static void ExtractCum_SpawnCum_Postfix()
    {
        CumProducerForNextSpawn = null;
    }

    static void DeflateBucket_SpawnCum_Prefix(object __instance, object fluid)
    {
        var driver = __instance as JobDriver;
        Thing bucket = null;
        Pawn pawn = null;
        if (driver != null)
        {
            pawn = driver.pawn;
            bucket = driver.job?.GetTarget(TargetIndex.A).Thing;
        }
        var link = bucket?.TryGetComp<CompCumBucketLink>();
        if (link != null && pawn != null)
        {
            link.NotifyPawnUsed(pawn);
            if (link.IsMixed)
            {
                CumProducerForNextSpawn = null;
                return;
            }
        }
        CumProducerForNextSpawn = GetProducerFromBucketDeflate(__instance, fluid);
    }

    static void DeflateBucket_SpawnCum_Postfix()
    {
        CumProducerForNextSpawn = null;
    }

    /// <summary>
    /// From JobDriver_DeflateBucket instance and current fluid, get the pawn to use as producer
    /// by reading cumflationHediff's SourceStorage sources (same fluid, pick by weight).
    /// </summary>
    static Pawn GetProducerFromBucketDeflate(object driver, object fluid)
    {
        if (driver == null || fluid == null)
            return null;
        var deflateCleanType = AccessTools.TypeByName("Cumpilation.Leaking.JobDriver_DeflateClean");
        if (deflateCleanType == null)
            return null;
        var hediffField = AccessTools.Field(deflateCleanType, "cumflationHediff");
        if (hediffField == null)
            return null;
        var hediff = hediffField.GetValue(driver) as Hediff;
        if (hediff == null)
            return null;
        var compType = AccessTools.TypeByName("Cumpilation.Cumflation.HediffComp_SourceStorage");
        if (compType == null)
            return null;
        var tryGetComp = AccessTools.Method(typeof(Hediff), "TryGetComp");
        if (tryGetComp == null || !tryGetComp.IsGenericMethodDefinition)
            return null;
        var comp = tryGetComp.MakeGenericMethod(compType).Invoke(hediff, null);
        if (comp == null)
            return null;
        var sourcesField = AccessTools.Field(compType, "sources");
        if (sourcesField == null)
            return null;
        var sources = sourcesField.GetValue(comp) as System.Collections.IList;
        if (sources == null || sources.Count == 0)
            return null;
        var fluidSourceType = AccessTools.TypeByName("Cumpilation.Cumflation.FluidSource");
        if (fluidSourceType == null)
            return null;
        var pawnField = AccessTools.Field(fluidSourceType, "pawn");
        var fluidField = AccessTools.Field(fluidSourceType, "fluid");
        var amountField = AccessTools.Field(fluidSourceType, "amount");
        if (pawnField == null || fluidField == null || amountField == null)
            return null;

        var withThisFluid = new List<(Pawn p, float w)>();
        for (int i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            if (s == null)
                continue;
            var f = fluidField.GetValue(s);
            if (f == null || !ReferenceEquals(f, fluid))
                continue;
            var p = pawnField.GetValue(s) as Pawn;
            var a = amountField.GetValue(s);
            float w = a is float fl ? fl : 0f;
            if (p != null && w > 0f)
                withThisFluid.Add((p, w));
        }
        if (withThisFluid.Count == 0)
            return null;
        if (withThisFluid.Count == 1)
            return withThisFluid[0].p;
        return withThisFluid.RandomElementByWeight(x => x.w).p;
    }

    static void ThingMaker_MakeThing_Postfix(Thing __result)
    {
        if (__result == null || __result.def?.defName != "Cumpilation_Cum" || CumProducerForNextSpawn == null)
            return;
        var comp = __result.TryGetComp<CompShowProducer>();
        if (comp != null)
            comp.producer = CumProducerForNextSpawn;
    }
}
