using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace EqualMilking.HarmonyPatches;

/// <summary>
/// When Cumpilation is loaded: tag cum items with producer so consumption
/// uses the same "who can eat" rules as milk (CompShowProducer + allowedConsumers).
/// - Recipe_ExtractCum: producer = extracted pawn.
/// - JobDriver_DeflateBucket (from bucket): producer = chosen FluidSource.pawn for that fluid.
/// </summary>
public static class CumpilationIntegration
{
    internal static Pawn CumProducerForNextSpawn;

    public static void ApplyPatches(Harmony harmony)
    {
        if (ModLister.GetActiveModWithIdentifier("vegapnk.cumpilation") == null)
            return;

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
