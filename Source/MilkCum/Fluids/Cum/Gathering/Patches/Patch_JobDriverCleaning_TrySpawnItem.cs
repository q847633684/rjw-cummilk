using HarmonyLib;
using MilkCum.Harmony;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace MilkCum.Fluids.Cum.Gathering
{

    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))] 
    public class Patch_JobDriverCleaning_TrySpawnItem
    {

        public static void Postfix(JobDriver __instance)
        {
            if (!Settings.EnableFluidGatheringWhileCleaning) return;

            if (__instance is JobDriver_CleanFilth cleanJob && cleanJob.ended)
            {
                var type = cleanJob.GetType();
                if (type == null) return;

                var property = type.GetProperty("Filth", bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null) return;

                var rawValue = property.GetValue(cleanJob, null);
                if (rawValue == null) return;

                Filth filth = (Filth)rawValue;
                if (filth == null) return;

                var gDef = GatheringUtility.LookupGatheringDef(filth.def);
                if (gDef == null || gDef.filthNecessaryForOneUnit <= 0) return;
                float chance = 1.0f / gDef.filthNecessaryForOneUnit;

                //ModLog.Debug($"Running JobDriver_CleanFilth Cleanup - {cleanJob.pawn} cleaned {filth} which is supported");

                if (Rand.Chance(chance))
                {
                    // 与 PassiveFluidGatherer 一致：从 FilthProducerRegistry 取产主，供 ThingMaker postfix 写入 CompShowProducer
                    CumpilationIntegration.PushCumProducerContext(FilthProducerRegistry.GetAndRemove(filth.Map, filth.Position, filth.def));
                    try
                    {
                        var result = ThingMaker.MakeThing(gDef.thingDef);
                        result.stackCount = 1;
                        GenPlace.TryPlaceThing(result, __instance.pawn.PositionHeld, __instance.pawn.Map, ThingPlaceMode.Direct, out _);
                    }
                    finally
                    {
                        CumpilationIntegration.PopCumProducerContext();
                    }
                }
            }
        }

    }
}
